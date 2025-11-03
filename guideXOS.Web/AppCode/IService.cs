using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using GuideXOS;

// NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IService" in both code and config file together.
[ServiceContract]
public interface IService
{

	[OperationContract]
	string GetData(int value);

	[OperationContract]
	CompositeType GetDataUsingDataContract(CompositeType composite);

    // Credentials
    [OperationContract]
    LoginResponse Register(RegisterRequest request);

    [OperationContract]
    LoginResponse Login(LoginRequest request);

    // Desktop text files
    [OperationContract]
    void SaveDesktopTextFile(SaveDesktopFileRequest request);

    [OperationContract]
    List<DesktopFileInfo> ListDesktopTextFiles(Guid loginGuid);

    [OperationContract]
    DesktopFileInfo GetDesktopTextFile(Guid loginGuid, string fileName);

    // Cloud VFS endpoints (simplified)
    [OperationContract]
    CloudListResponse CloudList(Guid loginGuid, string path, bool recursive, int pageSize, string continuationToken);

    [OperationContract]
    byte[] CloudRead(Guid loginGuid, string path);

    [OperationContract]
    CloudWriteResult CloudWrite(Guid loginGuid, string path, byte[] content, bool createParents);

    [OperationContract]
    void CloudDelete(Guid loginGuid, string path);
}

// Use a data contract as illustrated in the sample below to add composite types to service operations.
[DataContract]
public class CompositeType
{
	bool boolValue = true;
	string stringValue = "Hello ";

	[DataMember]
	public bool BoolValue
	{
		get { return boolValue; }
		set { boolValue = value; }
	}

	[DataMember]
	public string StringValue
	{
		get { return stringValue; }
		set { stringValue = value; }
	}
}

[DataContract]
public class CloudEntryInfo
{
    [DataMember]
    public string Path { get; set; }
    [DataMember]
    public string Name { get; set; }
    [DataMember]
    public string Type { get; set; } // "File" or "Directory"
}

[DataContract]
public class CloudListResponse
{
    [DataMember]
    public List<CloudEntryInfo> Items { get; set; }
    [DataMember]
    public string ContinuationToken { get; set; }
}

[DataContract]
public class CloudWriteResult
{
    [DataMember]
    public string Path { get; set; }
    [DataMember]
    public long Size { get; set; }
    [DataMember]
    public string ETag { get; set; }
}
