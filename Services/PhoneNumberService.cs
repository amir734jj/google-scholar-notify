using PhoneNumbers;
using ScholarNotify.Interfaces;

namespace ScholarNotify.Services;

public sealed class PhoneNumberService : IApplicationService
{
    private static readonly PhoneNumberUtil PhoneNumberUtil = PhoneNumberUtil.GetInstance();

    public string NormalizeToE164(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Enter a phone number.", nameof(value));
        }

        try
        {
            var phoneNumber = PhoneNumberUtil.Parse(value, "ZZ");
            if (!PhoneNumberUtil.IsValidNumber(phoneNumber))
            {
                throw new ArgumentException("Enter a valid international phone number, including the country code.", nameof(value));
            }

            return PhoneNumberUtil.Format(phoneNumber, PhoneNumberFormat.E164);
        }
        catch (NumberParseException exception)
        {
            throw new ArgumentException(
                "Enter a valid international phone number, including the country code.",
                nameof(value),
                exception);
        }
    }
}