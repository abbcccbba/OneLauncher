using SecureLocalStorage;
using System;
using System.Collections.Generic;
using System.Text;

namespace OneLauncher.Core.Net.msa;

public class SystemEC
{
    private SecureLocalStorage.SecureLocalStorage storage;
    /// <summary>
    /// 用于利用操作系统级密钥保护存储用户信息，全局单例
    /// </summary>
    public SystemEC()
    {
        var config = new CustomLocalStorageConfig(null, Init.ApplicationUUID).WithDefaultKeyBuilder();
        storage = new SecureLocalStorage.SecureLocalStorage(config);
    }
    public string GetRefreshToken(string tokenID)
    {
        if (tokenID == null)
            return UserModel.nullToken;
        return storage.Get(tokenID);
    }
    public string SetRefreshToken(string refreshToken,string oldID = null)
    {
        if (refreshToken == null)
            return UserModel.nullToken;
        if (!string.IsNullOrEmpty(oldID))
        {
            storage.Set(oldID, refreshToken);
            return oldID;
        }
        string ID = Guid.NewGuid().ToString();
        storage.Set(ID, refreshToken);
        return ID;
    }
    public void Del(string ID)
    {
        if (ID == null)
            return;
        storage.Remove(ID);
    }
}
