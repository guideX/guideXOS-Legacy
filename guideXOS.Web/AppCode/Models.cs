using System;
using System.Runtime.Serialization;

namespace GuideXOS
{
    // Database models
    public class UserCredential
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public Guid LoginGuid { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime? LastLoginUtc { get; set; }
    }

    public class DesktopFile
    {
        public int Id { get; set; }
        public Guid OwnerLoginGuid { get; set; }
        public string FileName { get; set; }
        public string Content { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
    }

    // WCF DataContracts
    [DataContract]
    public class RegisterRequest
    {
        [DataMember]
        public string Username { get; set; }
        [DataMember]
        public string Password { get; set; }
    }

    [DataContract]
    public class LoginRequest
    {
        [DataMember]
        public string Username { get; set; }
        [DataMember]
        public string Password { get; set; }
    }

    [DataContract]
    public class LoginResponse
    {
        [DataMember]
        public bool Success { get; set; }
        [DataMember]
        public string Message { get; set; }
        [DataMember]
        public Guid LoginGuid { get; set; }
    }

    [DataContract]
    public class SaveDesktopFileRequest
    {
        [DataMember]
        public Guid LoginGuid { get; set; }
        [DataMember]
        public string FileName { get; set; }
        [DataMember]
        public string Content { get; set; }
    }

    [DataContract]
    public class DesktopFileInfo
    {
        [DataMember]
        public string FileName { get; set; }
        [DataMember]
        public string Content { get; set; }
        [DataMember]
        public DateTime UpdatedUtc { get; set; }
    }
}
