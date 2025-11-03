using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using GuideXOS;

// NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Service" in code, svc and config file together.
public class Service : IService
{
    private readonly Repository _repo = new Repository();

	public string GetData(int value)
	{
		return string.Format("You entered: {0}", value);
	}

	public CompositeType GetDataUsingDataContract(CompositeType composite)
	{
		if (composite == null)
		{
			throw new ArgumentNullException("composite");
		}
		if (composite.BoolValue)
		{
			composite.StringValue += "Suffix";
		}
		return composite;
	}

    public LoginResponse Register(RegisterRequest request)
    {
        if (request == null) throw new ArgumentNullException("request");
        Guid token; string message;
        var ok = _repo.Register(request.Username, request.Password, out token, out message);
        return new LoginResponse { Success = ok, Message = message, LoginGuid = token };
    }

    public LoginResponse Login(LoginRequest request)
    {
        if (request == null) throw new ArgumentNullException("request");
        Guid token; string message;
        var ok = _repo.Login(request.Username, request.Password, out token, out message);
        return new LoginResponse { Success = ok, Message = message, LoginGuid = token };
    }

    public void SaveDesktopTextFile(SaveDesktopFileRequest request)
    {
        if (request == null) throw new ArgumentNullException("request");
        _repo.SaveDesktopText(request.LoginGuid, request.FileName, request.Content);
    }

    public List<DesktopFileInfo> ListDesktopTextFiles(Guid loginGuid)
    {
        var items = _repo.ListDesktopFiles(loginGuid);
        var list = new List<DesktopFileInfo>();
        foreach (var f in items)
        {
            list.Add(new DesktopFileInfo { FileName = f.FileName, Content = f.Content, UpdatedUtc = f.UpdatedUtc });
        }
        return list;
    }

    public DesktopFileInfo GetDesktopTextFile(Guid loginGuid, string fileName)
    {
        var f = _repo.GetDesktopFile(loginGuid, fileName);
        if (f == null) return null;
        return new DesktopFileInfo { FileName = f.FileName, Content = f.Content, UpdatedUtc = f.UpdatedUtc };
    }

    // ---------------- Cloud VFS methods ----------------
    public CloudListResponse CloudList(Guid loginGuid, string path, bool recursive, int pageSize, string continuationToken)
    {
        // In this minimal impl, we treat DesktopFile table as a flat namespace rooted at '/'.
        // Directory semantics are simulated by FileName prefixes with '/'.
        var all = _repo.ListDesktopFiles(loginGuid);
        if (path == null) path = string.Empty;
        if (path.Length > 0 && path[path.Length - 1] != '/') path += "/";
        var items = new List<CloudEntryInfo>();
        var seenDirs = new HashSet<string>();
        foreach (var f in all)
        {
            if (!f.FileName.StartsWith(path, StringComparison.Ordinal)) continue;
            var rest = f.FileName.Substring(path.Length);
            int slash = rest.IndexOf('/');
            if (slash >= 0)
            {
                string dir = rest.Substring(0, slash);
                if (!seenDirs.Contains(dir)) { seenDirs.Add(dir); items.Add(new CloudEntryInfo { Path = path + dir, Name = dir, Type = "Directory" }); }
            }
            else
            {
                items.Add(new CloudEntryInfo { Path = path + rest, Name = rest, Type = "File" });
            }
        }
        // Ignore paging/continuation for simplicity in this sample
        return new CloudListResponse { Items = items, ContinuationToken = null };
    }

    public byte[] CloudRead(Guid loginGuid, string path)
    {
        if (string.IsNullOrEmpty(path)) return new byte[0];
        var f = _repo.GetDesktopFile(loginGuid, path.TrimStart('/'));
        if (f == null || f.Content == null) return new byte[0];
        return Encoding.UTF8.GetBytes(f.Content);
    }

    public CloudWriteResult CloudWrite(Guid loginGuid, string path, byte[] content, bool createParents)
    {
        if (string.IsNullOrEmpty(path)) throw new ArgumentNullException("path");
        string text = content != null ? Encoding.UTF8.GetString(content) : string.Empty;
        _repo.SaveDesktopText(loginGuid, path.TrimStart('/'), text);
        return new CloudWriteResult { Path = path, Size = (content?.Length ?? 0), ETag = "" };
    }

    public void CloudDelete(Guid loginGuid, string path)
    {
        if (string.IsNullOrEmpty(path)) return;
        path = path.TrimStart('/');
        // If path ends with '/', delete all with prefix. Else delete that file.
        if (path[path.Length - 1] == '/') _repo.DeleteByPrefix(loginGuid, path);
        else _repo.DeleteDesktopFile(loginGuid, path);
    }
}
