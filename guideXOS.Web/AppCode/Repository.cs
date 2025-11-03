using System;
using System.Security.Cryptography;
using System.Text;
using PetaPoco;
using GuideXOS;
using System.Collections.Generic;

public class Repository
{
    private const string ConnName = "GuideXOSDb";

    public Repository()
    {
        EnsureSchema();
    }

    private void EnsureSchema()
    {
        using (var db = new Database(ConnName))
        {
            // Create tables if not exist
            db.Execute(@"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='UserCredential' and xtype='U')
                         CREATE TABLE UserCredential(
                           Id INT IDENTITY(1,1) PRIMARY KEY,
                           Username NVARCHAR(200) NOT NULL UNIQUE,
                           PasswordHash NVARCHAR(200) NOT NULL,
                           LoginGuid UNIQUEIDENTIFIER NOT NULL,
                           CreatedUtc DATETIME2 NOT NULL,
                           LastLoginUtc DATETIME2 NULL
                         )");

            db.Execute(@"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='DesktopFile' and xtype='U')
                         CREATE TABLE DesktopFile(
                           Id INT IDENTITY(1,1) PRIMARY KEY,
                           OwnerLoginGuid UNIQUEIDENTIFIER NOT NULL,
                           FileName NVARCHAR(255) NOT NULL,
                           Content NVARCHAR(MAX) NULL,
                           CreatedUtc DATETIME2 NOT NULL,
                           UpdatedUtc DATETIME2 NOT NULL
                         );
                         CREATE UNIQUE INDEX IX_DesktopFile_Owner_File ON DesktopFile(OwnerLoginGuid, FileName);");
        }
    }

    public bool Register(string username, string password, out Guid loginGuid, out string message)
    {
        loginGuid = Guid.Empty;
        message = null;
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
        {
            message = "Username and password required.";
            return false;
        }
        using (var db = new Database(ConnName))
        {
            var exists = db.ExecuteScalar<int>("SELECT COUNT(1) FROM UserCredential WHERE Username=@0", username);
            if (exists > 0)
            {
                message = "Username already exists.";
                return false;
            }
            loginGuid = Guid.NewGuid();
            string hash = HashPassword(password);
            db.Execute("INSERT INTO UserCredential(Username, PasswordHash, LoginGuid, CreatedUtc) VALUES(@0,@1,@2,SYSUTCDATETIME())", username, hash, loginGuid);
            return true;
        }
    }

    public bool Login(string username, string password, out Guid loginGuid, out string message)
    {
        loginGuid = Guid.Empty;
        message = null;
        using (var db = new Database(ConnName))
        {
            var rec = db.SingleOrDefault<UserCredential>("SELECT TOP 1 * FROM UserCredential WHERE Username=@0", username);
            if (rec == null)
            {
                message = "Invalid username or password.";
                return false;
            }
            if (!VerifyPassword(password, rec.PasswordHash))
            {
                message = "Invalid username or password.";
                return false;
            }
            // Issue a new LoginGuid on each login for simplicity
            rec.LoginGuid = Guid.NewGuid();
            db.Execute("UPDATE UserCredential SET LoginGuid=@0, LastLoginUtc=SYSUTCDATETIME() WHERE Id=@1", rec.LoginGuid, rec.Id);
            loginGuid = rec.LoginGuid;
            return true;
        }
    }

    public bool ValidateToken(Guid loginGuid)
    {
        if (loginGuid == Guid.Empty) return false;
        using (var db = new Database(ConnName))
        {
            var count = db.ExecuteScalar<int>("SELECT COUNT(1) FROM UserCredential WHERE LoginGuid=@0", loginGuid);
            return count > 0;
        }
    }

    public void SaveDesktopText(Guid loginGuid, string fileName, string content)
    {
        if (!ValidateToken(loginGuid)) throw new UnauthorizedAccessException("Invalid token");
        using (var db = new Database(ConnName))
        {
            var now = DateTime.UtcNow;
            // Try update first
            int rows = db.Execute("UPDATE DesktopFile SET Content=@0, UpdatedUtc=@1 WHERE OwnerLoginGuid=@2 AND FileName=@3", content, now, loginGuid, fileName);
            if (rows == 0)
            {
                db.Execute("INSERT INTO DesktopFile(OwnerLoginGuid, FileName, Content, CreatedUtc, UpdatedUtc) VALUES(@0,@1,@2,@3,@3)", loginGuid, fileName, content, now);
            }
        }
    }

    public List<DesktopFile> ListDesktopFiles(Guid loginGuid)
    {
        if (!ValidateToken(loginGuid)) throw new UnauthorizedAccessException("Invalid token");
        using (var db = new Database(ConnName))
        {
            var list = new List<DesktopFile>(db.Fetch<DesktopFile>("SELECT * FROM DesktopFile WHERE OwnerLoginGuid=@0 ORDER BY FileName", loginGuid));
            return list;
        }
    }

    public DesktopFile GetDesktopFile(Guid loginGuid, string fileName)
    {
        if (!ValidateToken(loginGuid)) throw new UnauthorizedAccessException("Invalid token");
        using (var db = new Database(ConnName))
        {
            return db.SingleOrDefault<DesktopFile>("SELECT TOP 1 * FROM DesktopFile WHERE OwnerLoginGuid=@0 AND FileName=@1", loginGuid, fileName);
        }
    }

    public int DeleteDesktopFile(Guid loginGuid, string fileName)
    {
        if (!ValidateToken(loginGuid)) throw new UnauthorizedAccessException("Invalid token");
        using (var db = new Database(ConnName))
        {
            return db.Execute("DELETE FROM DesktopFile WHERE OwnerLoginGuid=@0 AND FileName=@1", loginGuid, fileName);
        }
    }

    public int DeleteByPrefix(Guid loginGuid, string prefix)
    {
        if (!ValidateToken(loginGuid)) throw new UnauthorizedAccessException("Invalid token");
        using (var db = new Database(ConnName))
        {
            return db.Execute("DELETE FROM DesktopFile WHERE OwnerLoginGuid=@0 AND FileName LIKE @1", loginGuid, prefix + "%");
        }
    }

    private static string HashPassword(string password)
    {
        // PBKDF2
        using (var derive = new Rfc2898DeriveBytes(password, 16, 10000))
        {
            var salt = derive.Salt;
            var key = derive.GetBytes(32);
            var payload = new byte[1 + salt.Length + key.Length];
            payload[0] = 0x01; // version
            Buffer.BlockCopy(salt, 0, payload, 1, salt.Length);
            Buffer.BlockCopy(key, 0, payload, 1 + salt.Length, key.Length);
            return Convert.ToBase64String(payload);
        }
    }

    private static bool VerifyPassword(string password, string stored)
    {
        try
        {
            var payload = Convert.FromBase64String(stored);
            if (payload.Length < 1 + 16 + 32 || payload[0] != 0x01) return false;
            var salt = new byte[16];
            Buffer.BlockCopy(payload, 1, salt, 0, 16);
            var key = new byte[32];
            Buffer.BlockCopy(payload, 1 + 16, key, 0, 32);
            using (var derive = new Rfc2898DeriveBytes(password, salt, 10000))
            {
                var test = derive.GetBytes(32);
                return SlowEquals(key, test);
            }
        }
        catch
        {
            return false;
        }
    }

    private static bool SlowEquals(byte[] a, byte[] b)
    {
        uint diff = (uint)a.Length ^ (uint)b.Length;
        for (int i = 0; i < a.Length && i < b.Length; i++)
            diff |= (uint)(a[i] ^ b[i]);
        return diff == 0;
    }
}
