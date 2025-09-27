namespace YourApp.Shared.Constant;

public static class AuthMessages
{
    public static class Error
    {
        public const string InvalidGoogleToken = "Invalid Google token!";
        public const string GoogleLoginFailed = "Google login failed!";
        public const string AccountNotFound = "Account not found!";
        public const string InvalidPassword = "Invalid password!";
    }

    public static class Success
    {
        public const string GoogleLoginSuccess = "Google login successful!";
        public const string LoginSuccess = "Login successful!";
    }
}