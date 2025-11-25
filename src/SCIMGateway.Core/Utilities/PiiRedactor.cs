// ==========================================================================
// T026: PiiRedactor - PII Redaction for GDPR/CCPA Compliance
// ==========================================================================
// Redacts personally identifiable information from logs
// ==========================================================================

using System.Text.RegularExpressions;

namespace SCIMGateway.Core.Utilities;

/// <summary>
/// Interface for PII redaction.
/// </summary>
public interface IPiiRedactor
{
    /// <summary>
    /// Redacts PII from a string.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <returns>String with PII redacted.</returns>
    string Redact(string input);

    /// <summary>
    /// Redacts PII from a JSON string.
    /// </summary>
    /// <param name="json">The JSON string.</param>
    /// <returns>JSON string with PII redacted.</returns>
    string RedactJson(string json);

    /// <summary>
    /// Redacts an email address.
    /// </summary>
    /// <param name="email">The email address.</param>
    /// <returns>Partially masked email.</returns>
    string RedactEmail(string email);

    /// <summary>
    /// Redacts a phone number.
    /// </summary>
    /// <param name="phone">The phone number.</param>
    /// <returns>Partially masked phone number.</returns>
    string RedactPhone(string phone);

    /// <summary>
    /// Fully redacts an address.
    /// </summary>
    /// <param name="address">The address.</param>
    /// <returns>Fully redacted address.</returns>
    string RedactAddress(string address);
}

/// <summary>
/// PII redaction implementation.
/// </summary>
public partial class PiiRedactor : IPiiRedactor
{
    // Regex patterns for PII detection
    private static readonly Regex EmailPattern = EmailRegex();
    private static readonly Regex PhonePattern = PhoneRegex();
    private static readonly Regex SsnPattern = SsnRegex();
    private static readonly Regex CreditCardPattern = CreditCardRegex();
    
    // JSON field patterns for common PII fields
    private static readonly string[] PiiFieldNames =
    [
        "email", "emails", "emailAddress", "mail",
        "phone", "phones", "phoneNumber", "phoneNumbers", "mobile", "fax",
        "address", "addresses", "streetAddress", "postalCode", "zipCode",
        "ssn", "socialSecurityNumber", "taxId",
        "creditCard", "cardNumber", "cvv",
        "password", "secret", "token", "apiKey", "clientSecret",
        "birthDate", "dateOfBirth", "dob",
        "ipAddress", "ip"
    ];

    private const string RedactedPlaceholder = "[REDACTED]";
    private const string PartialRedactPlaceholder = "***";

    /// <inheritdoc />
    public string Redact(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var result = input;

        // Redact emails
        result = EmailPattern.Replace(result, match => RedactEmail(match.Value));

        // Redact phone numbers
        result = PhonePattern.Replace(result, match => RedactPhone(match.Value));

        // Redact SSNs
        result = SsnPattern.Replace(result, RedactedPlaceholder);

        // Redact credit card numbers
        result = CreditCardPattern.Replace(result, RedactedPlaceholder);

        return result;
    }

    /// <inheritdoc />
    public string RedactJson(string json)
    {
        if (string.IsNullOrEmpty(json))
            return json;

        var result = json;

        // Redact known PII fields in JSON
        foreach (var fieldName in PiiFieldNames)
        {
            // Match patterns like "fieldName": "value" or "fieldName": {...}
            var stringValuePattern = new Regex(
                $"\"{fieldName}\"\\s*:\\s*\"([^\"]+)\"",
                RegexOptions.IgnoreCase);
            
            result = stringValuePattern.Replace(result, match =>
            {
                var originalValue = match.Groups[1].Value;
                var redactedValue = GetRedactedValue(fieldName, originalValue);
                return $"\"{fieldName}\": \"{redactedValue}\"";
            });
        }

        // Also apply general redaction
        result = Redact(result);

        return result;
    }

    /// <inheritdoc />
    public string RedactEmail(string email)
    {
        if (string.IsNullOrEmpty(email))
            return email;

        var parts = email.Split('@');
        if (parts.Length != 2)
            return RedactedPlaceholder;

        var localPart = parts[0];
        var domain = parts[1];

        // Show first and last character of local part, mask the rest
        if (localPart.Length <= 2)
        {
            return $"{PartialRedactPlaceholder}@{domain}";
        }

        var maskedLocal = $"{localPart[0]}{PartialRedactPlaceholder}{localPart[^1]}";
        return $"{maskedLocal}@{domain}";
    }

    /// <inheritdoc />
    public string RedactPhone(string phone)
    {
        if (string.IsNullOrEmpty(phone))
            return phone;

        // Remove non-digits for processing
        var digits = new string(phone.Where(char.IsDigit).ToArray());

        if (digits.Length < 4)
            return RedactedPlaceholder;

        // Show only last 4 digits
        return $"{PartialRedactPlaceholder}{digits[^4..]}";
    }

    /// <inheritdoc />
    public string RedactAddress(string address)
    {
        // Full redaction for addresses per GDPR/CCPA
        if (string.IsNullOrEmpty(address))
            return address;

        return RedactedPlaceholder;
    }

    private string GetRedactedValue(string fieldName, string value)
    {
        var lowerFieldName = fieldName.ToLowerInvariant();

        return lowerFieldName switch
        {
            "email" or "emails" or "emailaddress" or "mail" => RedactEmail(value),
            "phone" or "phones" or "phonenumber" or "phonenumbers" or "mobile" or "fax" => RedactPhone(value),
            "address" or "addresses" or "streetaddress" => RedactAddress(value),
            "password" or "secret" or "token" or "apikey" or "clientsecret" or "cvv" => RedactedPlaceholder,
            "ssn" or "socialsecuritynumber" or "taxid" or "creditcard" or "cardnumber" => RedactedPlaceholder,
            "postalcode" or "zipcode" => PartialMaskPostalCode(value),
            "birthdate" or "dateofbirth" or "dob" => RedactedPlaceholder,
            "ipaddress" or "ip" => PartialMaskIp(value),
            _ => RedactedPlaceholder
        };
    }

    private static string PartialMaskPostalCode(string postalCode)
    {
        if (string.IsNullOrEmpty(postalCode) || postalCode.Length < 3)
            return RedactedPlaceholder;

        // Show first 3 characters for general area
        return $"{postalCode[..3]}{PartialRedactPlaceholder}";
    }

    private static string PartialMaskIp(string ip)
    {
        if (string.IsNullOrEmpty(ip))
            return ip;

        var parts = ip.Split('.');
        if (parts.Length != 4)
        {
            // IPv6 or invalid - full redaction
            return RedactedPlaceholder;
        }

        // Mask last two octets
        return $"{parts[0]}.{parts[1]}.{PartialRedactPlaceholder}.{PartialRedactPlaceholder}";
    }

    // Regex patterns
    [GeneratedRegex(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}", RegexOptions.Compiled)]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"(\+?1?[-.\s]?)?\(?[0-9]{3}\)?[-.\s]?[0-9]{3}[-.\s]?[0-9]{4}", RegexOptions.Compiled)]
    private static partial Regex PhoneRegex();

    [GeneratedRegex(@"\b\d{3}[-.]?\d{2}[-.]?\d{4}\b", RegexOptions.Compiled)]
    private static partial Regex SsnRegex();

    [GeneratedRegex(@"\b\d{4}[-.\s]?\d{4}[-.\s]?\d{4}[-.\s]?\d{4}\b", RegexOptions.Compiled)]
    private static partial Regex CreditCardRegex();
}
