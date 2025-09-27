namespace YourApp.Shared.Constant;

public static class UserMessages
{
    public static class Error
    {
        public const string UserNotFound = "User not found!";
        public const string EmailAlreadyExists = "Email already exists!";
        public const string InvalidUserId = "Invalid user ID!";
    }
    
    public static class Success
    {
        public const string UserCreated = "User created successfully!";
        public const string UserUpdated = "User updated successfully!";
        public const string UserDeleted = "User deleted successfully!";
        public const string UsersRetrieved = "Users retrieved successfully!";
    }
}