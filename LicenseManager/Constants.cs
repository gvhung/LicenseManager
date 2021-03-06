﻿namespace LicenseManager
{
    public class Constants
    {
        public const string AdminRoleName = "Admins";

        public const string CurrentUserOwinEnvironmentKey = "licensemanager.user";
        public const int DefaultPasswordResetTokenExpirationHours = 24;

        public const string Sha1HashAlgorithmId = "SHA1";
        public const string Sha512HashAlgorithmId = "SHA512";
        public const string PBKDF2HashAlgorithmId = "PBKDF2";

        public const string ReturnUrlViewDataKey = "ReturnUrl";

        public const string UploadFileNameTemplate = "{0}{1}";
        public const string UploadsFolderName = "uploads";
        public const string DownloadsFolderName = "downloads";
        public const string ContentFolderName = "content";

        public const string OctetStreamContentType = "application/octet-stream";
    }
}