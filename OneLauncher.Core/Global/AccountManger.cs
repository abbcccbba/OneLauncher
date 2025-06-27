using OneLauncher.Core.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OneLauncher.Core.Global;
[JsonSerializable(typeof(AccountData))]
[JsonSerializable(typeof(Dictionary<Guid,UserModel>))]
public partial class AccountDataJsonContext : JsonSerializerContext { }
public class AccountData
{
    public AccountData() {
        UserDictionary = new Dictionary<Guid, UserModel>();
    }
    public Dictionary<Guid, UserModel> UserDictionary { get; set; }
    public Guid? DefaultUserID { get; set; }
}
public class AccountManager
{
    private readonly string _configPath;
    private Dictionary<Guid, UserModel> _userDictionary = new();
    private Guid? _defaultUserId;

    private AccountManager(string configPath,AccountData accountData)
    {
        _configPath = configPath;
        _userDictionary = accountData.UserDictionary;
        _defaultUserId = accountData.DefaultUserID;
        if (_userDictionary.Count == 0)
        {
            var tmp = Guid.NewGuid();
            _userDictionary[tmp] = new UserModel(tmp, "default",new Guid(UserModel.nullToken));
            _defaultUserId = tmp;
            SaveAsync();
        }
    }

    public static async Task<AccountManager> InitializeAsync(string basePath)
    {
        var configPath = Path.Combine(basePath, "playerdata","account.json");
        Directory.CreateDirectory(Path.Combine(basePath,"playerdata"));
        if (!File.Exists(configPath))
        {
            var r = new AccountData();
            using (var fs = new FileStream(configPath, FileMode.Create, FileAccess.Write,FileShare.ReadWrite,0,true))
                await JsonSerializer.SerializeAsync<AccountData>(fs, r, AccountDataJsonContext.Default.AccountData);
            return new AccountManager(configPath, r);
        }
        try
        {
            AccountData data = await JsonSerializer.DeserializeAsync<AccountData>(File.OpenRead(configPath),AccountDataJsonContext.Default.AccountData);
            return new AccountManager(configPath, data);
        }
        catch (Exception ex)
        {
            throw new OlanException("加载账户失败", "accounts.json 文件可能已损坏。", OlanExceptionAction.FatalError, ex);
        }
    }
    public async Task SaveAsync()
    {
        // 序列化包装类 AccountData 
        var dataToSave = new AccountData
        {
            UserDictionary = _userDictionary,
            DefaultUserID = _defaultUserId
        };
        // 覆盖而不是写入
        using FileStream createStream = new FileStream(_configPath,FileMode.Create,FileAccess.Write,FileShare.None,0,true);
        // 避免资源被提前释放
            await JsonSerializer.SerializeAsync(createStream, dataToSave, AccountDataJsonContext.Default.AccountData);
    }
    public UserModel? GetUser(Guid id) => _userDictionary.GetValueOrDefault(id);
    public IEnumerable<UserModel> GetAllUsers() => _userDictionary.Values;
    public UserModel? GetDefaultUser() => _userDictionary.GetValueOrDefault(_defaultUserId ?? throw new OlanException("内部错误","尚未指定默认用户"));
    public Task AddUserAsync(UserModel user)
    {
        _userDictionary[user.UserID] = user;
        return SaveAsync();
    }

    public async Task RemoveUserAsync(Guid userId)
    {
        if (_userDictionary.Count == 1)
            throw new OlanException("拒绝访问","你至少要有一个有效的用户模型");
        if (_userDictionary.Remove(userId))
        {
            if (_defaultUserId == userId)
            {
                _defaultUserId = _userDictionary.Keys.FirstOrDefault();
            }
            await SaveAsync();
        }
    }

    public async Task SetDefaultAsync(Guid userId)
    {
        // 确保该用户存在于字典中
        if (_userDictionary.ContainsKey(userId))
        {
            _defaultUserId = userId;
            await SaveAsync();
        }
    }
}
