namespace Snake.Domain
{
    public class ExceptionErrorHelper
    {
        public const int UnknownError = 1;
        public const int CertificateTypeNotSupported = 2;
        public const int FileNotSpecifiedOrEmpty = 3;
        public const int ServerNotInitializedProperly = 4;
        public const int FileCorrupted = 5;
        public const int RecipientInvalid = 6;
        public const int CertificateValidateException = 7;
        public const int InputFileTooBig = 8;
        public const int KeyGenerationFailure = 9;
        public const int LinkCertificateToPrivateKeyFailure = 10;
        public const int RaApiCallFailure = 11;
        public const int EnrollmentRequestGenerationFailure = 12;
        public const int MailSendFailure = 13;
        public const int FileIncorrectContentType = 14;
        public const int TimedOutExceptionCode = -2147467259;
    }
}