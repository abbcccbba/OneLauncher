using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Core;

public static class Download
{
    public static async Task DownloadToMinecraft(NdDowItem FDI)
    {
        try
        {
            // 检查文件是否已存在，避免重复下载
            if (File.Exists(FDI.path))
            {
                return;
            }
            // 创建文件夹
            string directoryPath = Path.GetDirectoryName(FDI.path);
            if (!string.IsNullOrEmpty(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }


            // 下载并写入文件
            using (HttpClient client = new HttpClient())
            using (HttpResponseMessage response = await client.GetAsync(FDI.url, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();
                using (FileStream stream = new FileStream(FDI.path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite, bufferSize: 25565, useAsync: true))
                {
                    using (var httpStream = await response.Content.ReadAsStreamAsync())
                    {
                        await httpStream.CopyToAsync(stream, 25565); // 指定缓冲区大小
                    }
                    stream.Position = 0; //重置流位置到开头
                                            // SHA1校验
                    using (SHA1 sha1Hash = SHA1.Create())
                    {
                        byte[] hash = await sha1Hash.ComputeHashAsync(stream); 
                        string calculatedSha1 = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                        if (!string.Equals(calculatedSha1, FDI.sha1, StringComparison.OrdinalIgnoreCase))
                        {
                            throw new InvalidDataException($"SHA1校验失败。文件: {FDI.path}, 预期: {FDI.sha1}, 实际: {calculatedSha1}");
                        }
                    }
                }
            }
        }
        catch (HttpRequestException ex)
        {
            Debug.WriteLine($"网络请求失败: {ex.Message}, StatusCode: {ex.StatusCode}, URL: {FDI.url}");
            throw;
        }
        catch (IOException ex)
        {
            Debug.WriteLine($"文件操作失败: {ex.Message}, 路径: {FDI.path}");
            throw;
        }
        catch (OperationCanceledException ex)
        {
            Debug.WriteLine($"下载超时或被取消: {ex.Message}, URL: {FDI.url}");
            throw;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"下载文件时发生未知错误: {ex}, URL: {FDI.url}, 路径: {FDI.path}");
            throw;
        }
    }
    public static async Task DownloadToMinecraft(
        List<NdDowItem> FDI, 
        IProgress<(int downloadedFiles, int totalFiles, int verifiedFiles)> progress = null,
        int maxConcurrentDownloads = 16, 
        int maxConcurrentSha1 = 16,
        bool IsSha1 = true,
        bool IsCheckFileExists = true
        )
    {
        try
        {
            // 检查文件是否已存在并创建文件夹
            var filesToDownload = new List<NdDowItem>();
            if (IsCheckFileExists)
            foreach (var item in FDI)
            {
                if (File.Exists(item.path)) continue;
                string directoryPath = Path.GetDirectoryName(item.path);
                if (!string.IsNullOrEmpty(directoryPath)) Directory.CreateDirectory(directoryPath);
                filesToDownload.Add(item);
            } else filesToDownload = FDI;

            int downloadedFiles = 0;
            int verifiedFiles = 0;
            int totalFiles = filesToDownload.Count;

            // 报告初始进度
            progress?.Report((downloadedFiles, totalFiles, verifiedFiles));

            // 下载阶段
            using (var client = new HttpClient(new HttpClientHandler
            { MaxConnectionsPerServer = maxConcurrentDownloads })
            { Timeout = TimeSpan.FromSeconds(30) })
            {
                //client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

                // 并发下载文件
                var semaphore = new SemaphoreSlim(maxConcurrentDownloads);
                var downloadTasks = new List<Task>();
                foreach (var item in filesToDownload)
                {
                    await semaphore.WaitAsync();
                    downloadTasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            // 下载并写入文件（带重试）
                            for (int attempt = 0; attempt < 3; attempt++)
                            {
                                try
                                {
                                    using (var response = await client.GetAsync(item.url, HttpCompletionOption.ResponseHeadersRead))
                                    {
                                        response.EnsureSuccessStatusCode();
                                        using (var httpStream = await response.Content.ReadAsStreamAsync())
                                        using (var fileStream = new FileStream(item.path, FileMode.Create, FileAccess.Write, FileShare.Write, bufferSize: 8192, useAsync: true))
                                        {
                                            await httpStream.CopyToAsync(fileStream, 8192);
                                            Interlocked.Increment(ref downloadedFiles);
                                            progress?.Report((downloadedFiles, totalFiles, verifiedFiles));
                                        }
                                    }
                                    break; // 成功后退出重试
                                }
                                catch (HttpRequestException ex) when (attempt < 2)
                                {
                                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"下载 {item.url} 时发生错误: {ex.Message}");
                            throw;
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }));
                }

                // 等待所有下载任务完成
                await Task.WhenAll(downloadTasks);
            } 
            

            // 校验阶段：并发校验 SHA1
            if(IsSha1)
            {
                var semaphore = new SemaphoreSlim(maxConcurrentSha1); 
                var sha1Tasks = new List<Task>();
                foreach (var item in filesToDownload)
                {
                    await semaphore.WaitAsync();
                    sha1Tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            using (var stream = new FileStream(item.path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 8192, useAsync: true))
                            using (var sha1Hash = SHA1.Create())
                            {
                                byte[] hash = await sha1Hash.ComputeHashAsync(stream);
                                string calculatedSha1 = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                                Interlocked.Increment(ref verifiedFiles);
                                progress?.Report((downloadedFiles, totalFiles, verifiedFiles));
                                if (!string.Equals(calculatedSha1, item.sha1, StringComparison.OrdinalIgnoreCase))
                                {
                                    throw new InvalidDataException($"SHA1 校验失败。文件: {item.path}, 预期: {item.sha1}, 实际: {calculatedSha1}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            throw;
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }));
                }

                // 等待所有 SHA1 校验任务完成
                await Task.WhenAll(sha1Tasks);
            }
        }
        catch (HttpRequestException ex)
        {
            Debug.WriteLine($"网络请求失败: {ex.Message}, StatusCode: {ex.StatusCode}");
            throw;
        }
        catch (IOException ex)
        {
            Debug.WriteLine($"文件操作失败: {ex.Message}");
            throw;
        }
        catch (OperationCanceledException ex)
        {
            Debug.WriteLine($"下载超时或被取消: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"下载或校验时发生未知错误: {ex.Message}");
            throw;
        }
    }
    // 无sha1校验的重载
    public static async Task DownloadToMinecraft(string url, string path)
    {
        try
        {
            if (File.Exists(path))
                return;
                
            // 创建文件夹
            string directoryPath = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            // 下载并写入文件
            using (HttpClient client = new HttpClient())
            {
                using (HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    using (var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write))
                    {
                        using (var httpStream = await response.Content.ReadAsStreamAsync())
                        {
                            await httpStream.CopyToAsync(fileStream);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // 抛出原始错误
            throw;
        }
    }
}

