﻿using Bluetooth.Core.Services;
using Bluetooth.Services.Obex;
using DownloaderProvider;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Data.Sqlite;
using Microsoft.Toolkit.Uwp.Notifications;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TinyPinyin.Core;
using Windows.ApplicationModel.Core;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Media.Editing;
using Windows.Media.MediaProperties;
using Windows.Media.Transcoding;
using Windows.Networking;
using Windows.Networking.Connectivity;
using Windows.Security.Credentials;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Search;
using Windows.System.UserProfile;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Notifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using TreeView = Microsoft.UI.Xaml.Controls.TreeView;
using TreeViewItem = Microsoft.UI.Xaml.Controls.TreeViewItem;
using TreeViewNode = Microsoft.UI.Xaml.Controls.TreeViewNode;

namespace FileManager
{
    #region SQLite数据库
    public sealed class SQLite : IDisposable
    {
        private bool IsDisposed = false;
        private static SQLite SQL = null;
        private SqliteConnection OLEDB = new SqliteConnection("Filename=RX_Sqlite.db;");
        private SQLite()
        {
            SQLitePCL.Batteries_V2.Init();
            SQLitePCL.raw.sqlite3_win32_set_directory(1, ApplicationData.Current.LocalFolder.Path);
            SQLitePCL.raw.sqlite3_win32_set_directory(2, ApplicationData.Current.TemporaryFolder.Path);

            OLEDB.Open();
            string Command = @"Create Table If Not Exists SearchHistory (SearchText Text Not Null, Primary Key (SearchText));
                                   Create Table If Not Exists WebFavourite (Subject Text Not Null, WebSite Text Not Null, Primary Key (WebSite));
                                   Create Table If Not Exists WebHistory (Subject Text Not Null, WebSite Text Not Null, DateTime Text Not Null, Primary Key (Subject, WebSite, DateTime));
                                   Create Table If Not Exists DownloadHistory (UniqueID Text Not Null, ActualName Text Not Null, Uri Text Not Null, State Text Not Null, Primary Key(UniqueID));
                                   Create Table If Not Exists QuickStart (Name Text Not Null, FullPath Text Not Null, Protocal Text Not Null, Type Text Not Null, Primary Key (Name,FullPath,Protocal,Type));
                                   Create Table If Not Exists FolderLibrary (Path Text Not Null, Primary Key (Path));
                                   Create Table If Not Exists PathHistory (Path Text Not Null, Primary Key (Path));
                                   Create Table If Not Exists BackgroundPicture (FileName Text Not Null, Primary Key (FileName));";

            using (SqliteCommand CreateTable = new SqliteCommand(Command, OLEDB))
            {
                _ = CreateTable.ExecuteNonQuery();
            }

            InitializeBackgroundPicture();
        }

        public static SQLite Current
        {
            get
            {
                lock (SyncRootProvider.SyncRoot)
                {
                    return SQL ?? (SQL = new SQLite());
                }
            }
        }

        //private SqliteConnection CreateConnectionToDataBase()
        //{
        //    SqliteConnection Connection = new SqliteConnection("Filename=RX_Sqlite.db");

        //    Connection.Open();
        //    string Command = @"Create Table If Not Exists SearchHistory (SearchText Text Not Null, Primary Key (SearchText));
        //                           Create Table If Not Exists WebFavourite (Subject Text Not Null, WebSite Text Not Null, Primary Key (WebSite));
        //                           Create Table If Not Exists WebHistory (Subject Text Not Null, WebSite Text Not Null, DateTime Text Not Null, Primary Key (Subject, WebSite, DateTime));
        //                           Create Table If Not Exists DownloadHistory (UniqueID Text Not Null, ActualName Text Not Null, Uri Text Not Null, State Text Not Null, Primary Key(UniqueID));
        //                           Create Table If Not Exists QuickStart (Name Text Not Null, FullPath Text Not Null, Protocal Text Not Null, Type Text Not Null, Primary Key (Name,FullPath,Protocal,Type));
        //                           Create Table If Not Exists FolderLibrary (Path Text Not Null, Primary Key (Path));
        //                           Create Table If Not Exists PathHistory (Path Text Not Null, Primary Key (Path));
        //                           Create Table If Not Exists BackgroundPicture (FileName Text Not Null, Primary Key (FileName));";

        //    using (SqliteCommand CreateTable = new SqliteCommand(Command, Connection))
        //    {
        //        _ = CreateTable.ExecuteNonQuery();
        //    }
        //}

        private void InitializeBackgroundPicture()
        {
            string Command = @"Insert Or Ignore Into BackgroundPicture Values('ms-appx:///CustomImage/Picture1.jpg');
                               Insert Or Ignore Into BackgroundPicture Values('ms-appx:///CustomImage/Picture2.jpg');
                               Insert Or Ignore Into BackgroundPicture Values('ms-appx:///CustomImage/Picture3.jpg');
                               Insert Or Ignore Into BackgroundPicture Values('ms-appx:///CustomImage/Picture4.jpg');
                               Insert Or Ignore Into BackgroundPicture Values('ms-appx:///CustomImage/Picture5.jpg');
                               Insert Or Ignore Into BackgroundPicture Values('ms-appx:///CustomImage/Picture6.jpg');
                               Insert Or Ignore Into BackgroundPicture Values('ms-appx:///CustomImage/Picture7.jpg');
                               Insert Or Ignore Into BackgroundPicture Values('ms-appx:///CustomImage/Picture8.jpg');
                               Insert Or Ignore Into BackgroundPicture Values('ms-appx:///CustomImage/Picture9.jpg');
                               Insert Or Ignore Into BackgroundPicture Values('ms-appx:///CustomImage/Picture10.jpg');
                               Insert Or Ignore Into BackgroundPicture Values('ms-appx:///CustomImage/Picture11.jpg');
                               Insert Or Ignore Into BackgroundPicture Values('ms-appx:///CustomImage/Picture12.jpg');
                               Insert Or Ignore Into BackgroundPicture Values('ms-appx:///CustomImage/Picture13.jpg');
                               Insert Or Ignore Into BackgroundPicture Values('ms-appx:///CustomImage/Picture14.jpg');";

            using (SqliteCommand CreateTable = new SqliteCommand(Command, OLEDB))
            {
                _ = CreateTable.ExecuteNonQuery();
            }
        }

        public async Task SetBackgroundPictureAsync(string uri)
        {
            using (SqliteCommand Command = new SqliteCommand("Insert Into BackgroundPicture Values (@FileName)", OLEDB))
            {
                _ = Command.Parameters.AddWithValue("@FileName", uri);
                _ = await Command.ExecuteNonQueryAsync();
            }
        }

        public async Task<List<Uri>> GetBackgroundPictureAsync()
        {
            List<Uri> list = new List<Uri>();
            using (SqliteCommand Command = new SqliteCommand("Select * From BackgroundPicture", OLEDB))
            using (SqliteDataReader query = await Command.ExecuteReaderAsync())
            {
                while (query.Read())
                {
                    list.Add(new Uri(query[0].ToString()));
                }
            }
            return list;
        }

        public async Task DeleteBackgroundPictureAsync(string uri)
        {
            using (SqliteCommand Command = new SqliteCommand("Delete From BackgroundPicture Where FileName=@FileName", OLEDB))
            {
                _ = Command.Parameters.AddWithValue("@FileName", uri);
                _ = await Command.ExecuteNonQueryAsync();
            }
        }

        public async IAsyncEnumerable<string> GetFolderLibraryAsync()
        {
            using (SqliteCommand Command = new SqliteCommand("Select * From FolderLibrary", OLEDB))
            using (SqliteDataReader query = await Command.ExecuteReaderAsync())
            {
                while (query.Read())
                {
                    yield return query[0].ToString();
                }
            }
        }

        public async Task DeleteFolderLibraryAsync(string Path)
        {
            using (SqliteCommand Command = new SqliteCommand("Delete From FolderLibrary Where Path = @Path", OLEDB))
            {
                _ = Command.Parameters.AddWithValue("@Path", Path);
                _ = await Command.ExecuteNonQueryAsync();
            }
        }

        public async Task SetPathHistoryAsync(string Path)
        {
            using (SqliteCommand Command = new SqliteCommand("Insert Or Ignore Into PathHistory Values (@Para)", OLEDB))
            {
                _ = Command.Parameters.AddWithValue("@Para", Path);
                _ = await Command.ExecuteNonQueryAsync();
            }
        }

        public async Task<List<string>> GetRelatedPathHistoryAsync(string Target)
        {
            List<string> PathList = new List<string>();
            using (SqliteCommand Command = new SqliteCommand("Select * From PathHistory Where Path Like @Target", OLEDB))
            {
                _ = Command.Parameters.AddWithValue("@Target", "%" + Target + "%");
                using (SqliteDataReader query = await Command.ExecuteReaderAsync())
                {
                    while (query.Read())
                    {
                        PathList.Add(query[0].ToString());
                    }
                }
            }
            return PathList;
        }

        public async Task SetFolderLibraryAsync(string Path)
        {
            using (SqliteCommand Command = new SqliteCommand("Insert Into FolderLibrary Values (@Path)", OLEDB))
            {
                _ = Command.Parameters.AddWithValue("@Path", Path);
                _ = await Command.ExecuteNonQueryAsync();
            }
        }

        public async Task SetSearchHistoryAsync(string SearchText)
        {
            using (SqliteCommand Command = new SqliteCommand("Insert Or Ignore Into SearchHistory Values (@Para)", OLEDB))
            {
                _ = Command.Parameters.AddWithValue("@Para", SearchText);
                _ = await Command.ExecuteNonQueryAsync();
            }
        }

        public async Task SetQuickStartItemAsync(string Name, string FullPath, string Protocal, QuickStartType Type)
        {
            using (SqliteCommand Command = new SqliteCommand("Insert Or Ignore Into QuickStart Values (@Name,@Path,@Protocal,@Type)", OLEDB))
            {
                _ = Command.Parameters.AddWithValue("@Name", Name);
                _ = Command.Parameters.AddWithValue("@Path", FullPath);
                _ = Command.Parameters.AddWithValue("@Protocal", Protocal);
                _ = Command.Parameters.AddWithValue("@Type", Enum.GetName(typeof(QuickStartType), Type));
                _ = await Command.ExecuteNonQueryAsync();
            }
        }

        public async Task UpdateQuickStartItemAsync(string OldName, string NewName, string FullPath, string Protocal, QuickStartType Type)
        {
            if (FullPath != null)
            {
                using (SqliteCommand Command = new SqliteCommand("Update QuickStart Set Name=@NewName, FullPath=@Path, Protocal=@Protocal Where Name=@OldName And Type=@Type", OLEDB))
                {
                    _ = Command.Parameters.AddWithValue("@OldName", OldName);
                    _ = Command.Parameters.AddWithValue("@Path", FullPath);
                    _ = Command.Parameters.AddWithValue("@NewName", NewName);
                    _ = Command.Parameters.AddWithValue("@Protocal", Protocal);
                    _ = Command.Parameters.AddWithValue("@Type", Enum.GetName(typeof(QuickStartType), Type));
                    _ = await Command.ExecuteNonQueryAsync();
                }
            }
            else
            {
                using (SqliteCommand Command = new SqliteCommand("Update QuickStart Set Name=@NewName, Protocal=@Protocal Where Name=@OldName And Type=@Type", OLEDB))
                {
                    _ = Command.Parameters.AddWithValue("@OldName", OldName);
                    _ = Command.Parameters.AddWithValue("@NewName", NewName);
                    _ = Command.Parameters.AddWithValue("@Protocal", Protocal);
                    _ = Command.Parameters.AddWithValue("@Type", Enum.GetName(typeof(QuickStartType), Type));
                    _ = await Command.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task DeleteQuickStartItemAsync(QuickStartItem Item)
        {
            using (SqliteCommand Command = new SqliteCommand("Delete From QuickStart Where Name = @Name And FullPath = @FullPath And Type=@Type", OLEDB))
            {
                _ = Command.Parameters.AddWithValue("@Name", Item.DisplayName);
                _ = Command.Parameters.AddWithValue("@FullPath", Item.RelativePath);
                _ = Command.Parameters.AddWithValue("@Type", Enum.GetName(typeof(QuickStartType), Item.Type));
                _ = await Command.ExecuteNonQueryAsync();
            }
        }

        public async IAsyncEnumerable<KeyValuePair<QuickStartType, QuickStartItem>> GetQuickStartItemAsync()
        {
            using (SqliteCommand Command = new SqliteCommand("Select * From QuickStart", OLEDB))
            using (SqliteDataReader query = await Command.ExecuteReaderAsync())
            {
                while (query.Read())
                {
                    StorageFile ImageFile = null;
                    try
                    {
                        ImageFile = await StorageFile.GetFileFromPathAsync(Path.Combine(ApplicationData.Current.LocalFolder.Path, query[1].ToString()));
                    }
                    catch (Exception)
                    {
                        using (SqliteCommand Command1 = new SqliteCommand("Delete From QuickStart Where Name = @Name And FullPath = @FullPath And Type=@Type", OLEDB))
                        {
                            _ = Command1.Parameters.AddWithValue("@Name", query[0].ToString());
                            _ = Command1.Parameters.AddWithValue("@FullPath", query[1].ToString());
                            _ = Command1.Parameters.AddWithValue("@Type", query[3].ToString());
                            _ = await Command1.ExecuteNonQueryAsync();
                        }
                    }

                    if (ImageFile != null)
                    {
                        using (var Stream = await ImageFile.OpenAsync(FileAccessMode.Read))
                        {
                            BitmapImage Bitmap = new BitmapImage
                            {
                                DecodePixelHeight = 80,
                                DecodePixelWidth = 80
                            };
                            await Bitmap.SetSourceAsync(Stream);
                            if ((QuickStartType)Enum.Parse(typeof(QuickStartType), query[3].ToString()) == QuickStartType.Application)
                            {
                                yield return new KeyValuePair<QuickStartType, QuickStartItem>(QuickStartType.Application, new QuickStartItem(Bitmap, new Uri(query[2].ToString()), QuickStartType.Application, query[1].ToString(), query[0].ToString()));
                            }
                            else
                            {
                                yield return new KeyValuePair<QuickStartType, QuickStartItem>(QuickStartType.WebSite, new QuickStartItem(Bitmap, new Uri(query[2].ToString()), QuickStartType.WebSite, query[1].ToString(), query[0].ToString()));
                            }
                        }
                    }
                }
            }
        }

        public async Task<List<string>> GetRelatedSearchHistoryAsync(string Target)
        {
            List<string> HistoryList = new List<string>();
            using (SqliteCommand Command = new SqliteCommand("Select * From SearchHistory Where SearchText Like @Target", OLEDB))
            {
                _ = Command.Parameters.AddWithValue("@Target", "%" + Target + "%");
                using (SqliteDataReader query = await Command.ExecuteReaderAsync())
                {
                    while (query.Read())
                    {
                        HistoryList.Add(query[0].ToString());
                    }
                    return HistoryList;
                }
            }
        }

        public async Task GetDownloadHistoryAsync()
        {
            using (SqliteCommand Command = new SqliteCommand("Select * From DownloadHistory", OLEDB))
            using (SqliteDataReader query = await Command.ExecuteReaderAsync())
            {
                for (int i = 0; query.Read(); i++)
                {
                    DownloadState State = (DownloadState)Enum.Parse(typeof(DownloadState), query[3].ToString());
                    if (State == DownloadState.Downloading || State == DownloadState.Paused)
                    {
                        State = DownloadState.Canceled;
                    }

                    WebDownloader.DownloadList.Add(WebDownloader.CreateDownloadOperatorFromDatabase(new Uri(query[2].ToString()), query[1].ToString(), State, query[0].ToString())); ;
                }
            }
        }

        public async Task SetDownloadHistoryAsync(DownloadOperator Task)
        {
            using (SqliteCommand Command = new SqliteCommand("Insert Into DownloadHistory Values (@UniqueID,@ActualName,@Uri,@State)", OLEDB))
            {
                _ = Command.Parameters.AddWithValue("@UniqueID", Task.UniqueID);
                _ = Command.Parameters.AddWithValue("@ActualName", Task.ActualFileName);
                _ = Command.Parameters.AddWithValue("@Uri", Task.Address.AbsoluteUri);
                _ = Command.Parameters.AddWithValue("@State", Enum.GetName(typeof(DownloadState), Task.State));
                _ = await Command.ExecuteNonQueryAsync();
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "<挂起>")]
        public async Task ClearTableAsync(string TableName)
        {
            using (SqliteCommand Command = new SqliteCommand("Delete From " + TableName, OLEDB))
            {
                _ = await Command.ExecuteNonQueryAsync();
            }
        }

        public async Task DeleteDownloadHistoryAsync(DownloadOperator Task)
        {
            using (SqliteCommand Command = new SqliteCommand("Delete From DownloadHistory Where UniqueID = @UniqueID", OLEDB))
            {
                _ = Command.Parameters.AddWithValue("@UniqueID", Task.UniqueID);
                _ = await Command.ExecuteNonQueryAsync();
            }
        }

        public async Task UpdateDownloadHistoryAsync(DownloadOperator Task)
        {
            using (SqliteCommand Command = new SqliteCommand("Update DownloadHistory Set State = @State Where UniqueID = @UniqueID", OLEDB))
            {
                _ = Command.Parameters.AddWithValue("@UniqueID", Task.UniqueID);
                _ = Command.Parameters.AddWithValue("@State", Enum.GetName(typeof(DownloadState), Task.State));
                _ = await Command.ExecuteNonQueryAsync();
            }
        }

        public async Task SetWebFavouriteListAsync(WebSiteItem Info)
        {
            using (SqliteCommand Command = new SqliteCommand("Insert Into WebFavourite Values (@Subject,@WebSite)", OLEDB))
            {
                _ = Command.Parameters.AddWithValue("@Subject", Info.Subject);
                _ = Command.Parameters.AddWithValue("@WebSite", Info.WebSite);
                _ = await Command.ExecuteNonQueryAsync();
            }
        }

        public async Task DeleteWebFavouriteListAsync(WebSiteItem Info)
        {
            using (SqliteCommand Command = new SqliteCommand("Delete From WebFavourite Where WebSite = @WebSite", OLEDB))
            {
                _ = Command.Parameters.AddWithValue("@WebSite", Info.WebSite);
                _ = await Command.ExecuteNonQueryAsync();
            }
        }

        public void DeleteWebHistory(KeyValuePair<DateTime, WebSiteItem> Info)
        {
            using (SqliteCommand Command = new SqliteCommand("Delete From WebHistory Where Subject=@Subject And WebSite=@WebSite And DateTime=@DateTime", OLEDB))
            {
                _ = Command.Parameters.AddWithValue("@Subject", Info.Value.Subject);
                _ = Command.Parameters.AddWithValue("@WebSite", Info.Value.WebSite);
                _ = Command.Parameters.AddWithValue("@DateTime", Info.Key.ToBinary().ToString());

                _ = Command.ExecuteNonQuery();
            }
        }

        public void SetWebHistoryList(KeyValuePair<DateTime, WebSiteItem> Info)
        {
            using (SqliteCommand Command = new SqliteCommand("Insert Into WebHistory Values (@Subject,@WebSite,@DateTime)", OLEDB))
            {
                _ = Command.Parameters.AddWithValue("@Subject", Info.Value.Subject);
                _ = Command.Parameters.AddWithValue("@WebSite", Info.Value.WebSite);
                _ = Command.Parameters.AddWithValue("@DateTime", Info.Key.ToBinary().ToString());

                _ = Command.ExecuteNonQuery();
            }
        }

        public async Task<List<KeyValuePair<DateTime, WebSiteItem>>> GetWebHistoryListAsync()
        {
            using (SqliteCommand Command = new SqliteCommand("Select * From WebHistory", OLEDB))
            using (SqliteDataReader Query = await Command.ExecuteReaderAsync())
            {
                List<KeyValuePair<DateTime, WebSiteItem>> HistoryList = new List<KeyValuePair<DateTime, WebSiteItem>>();

                while (Query.Read())
                {
                    HistoryList.Add(new KeyValuePair<DateTime, WebSiteItem>(DateTime.FromBinary(Convert.ToInt64(Query[2])), new WebSiteItem(Query[0].ToString(), Query[1].ToString())));
                }

                HistoryList.Reverse();
                return HistoryList;
            }
        }

        public async Task<List<WebSiteItem>> GetWebFavouriteListAsync()
        {
            using (SqliteCommand Command = new SqliteCommand("Select * From WebFavourite", OLEDB))
            using (SqliteDataReader Query = await Command.ExecuteReaderAsync())
            {
                List<WebSiteItem> FavList = new List<WebSiteItem>();

                while (Query.Read())
                {
                    FavList.Add(new WebSiteItem(Query[0].ToString(), Query[1].ToString()));
                }

                return FavList;
            }
        }

        public async Task ClearSearchHistoryRecord()
        {
            using (SqliteCommand Command = new SqliteCommand("Delete From SearchHistory", OLEDB))
            {
                _ = await Command.ExecuteNonQueryAsync();
            }
        }

        public void Dispose()
        {
            if (!IsDisposed)
            {
                OLEDB.Dispose();
                OLEDB = null;
                SQL = null;
            }
            IsDisposed = true;
        }

        ~SQLite()
        {
            Dispose();
        }
    }
    #endregion

    #region MySQL数据库
    public sealed class MySQL : IDisposable
    {
        private static MySQL Instance;

        private bool IsDisposed = false;

        private AutoResetEvent ConnectionLocker;

        public static MySQL Current
        {
            get
            {
                lock (SyncRootProvider.SyncRoot)
                {
                    return Instance ?? (Instance = new MySQL());
                }
            }
        }

        private MySQL()
        {
            ConnectionLocker = new AutoResetEvent(true);
        }

        public Task<MySqlConnection> CreateConnectionToDataBaseAsync()
        {
            return Task.Run(() =>
            {
                ConnectionLocker.WaitOne();

                MySqlConnection Connection = new MySqlConnection("Data Source=zhuxb711.rdsmt2onuvpvh1v.rds.gz.baidubce.com;port=3306;CharSet=utf8;User id=zhuxb711;password=password123;Database=FeedBackDataBase;");

                try
                {
                    Connection.Open();
                    const string CommandText = @"Create Table If Not Exists FeedBackTable (UserName Text Not Null, Title Text Not Null, Suggestion Text Not Null, LikeNum Text Not Null, DislikeNum Text Not Null, UserID Text Not Null, GUID Text Not Null);
                                                 Create Table If Not Exists VoteRecordTable (UserID Text Not Null, GUID Text Not Null, Behavior Text Not Null)";
                    using (MySqlCommand Command = new MySqlCommand(CommandText, Connection))
                    {
                        _ = Command.ExecuteNonQuery();
                    }
                }
                finally
                {
                    ConnectionLocker.Set();
                }

                return Connection;
            });
        }

        public async IAsyncEnumerable<FeedBackItem> GetAllFeedBackAsync()
        {
            using (MySqlConnection Connection = await CreateConnectionToDataBaseAsync())
            {
                if (Connection.State == System.Data.ConnectionState.Open)
                {
                    using (MySqlCommand Command = new MySqlCommand("Select * From FeedBackTable", Connection))
                    using (DbDataReader Reader = await Command.ExecuteReaderAsync())
                    {
                        var CurrentLanguage = Globalization.Language;
                        while (await Reader.ReadAsync())
                        {
                            string TitleTranslation = await Reader["Title"].ToString().TranslateToAsync(CurrentLanguage);
                            string SuggestionTranslation = await Reader["Suggestion"].ToString().TranslateToAsync(CurrentLanguage);
                            yield return new FeedBackItem(CurrentLanguage == LanguageEnum.Chinese ? Reader["UserName"].ToString() : Reader["UserName"].ToString().TranslateToPinyinOrStayInEnglish(), string.IsNullOrEmpty(TitleTranslation) ? Reader["Title"].ToString() : TitleTranslation, string.IsNullOrEmpty(SuggestionTranslation) ? Reader["Suggestion"].ToString() : SuggestionTranslation, Reader["LikeNum"].ToString(), Reader["DislikeNum"].ToString(), Reader["UserID"].ToString(), Reader["GUID"].ToString());
                        }
                    }
                }
                else
                {
                    yield break;
                }
            }
        }

        public Task<bool> GetExtraFeedBackInfo(FeedBackItem list)
        {
            return CreateConnectionToDataBaseAsync().ContinueWith((task, Para) =>
            {
                using (MySqlConnection Connection = task.Result)
                {
                    if (Connection.State == System.Data.ConnectionState.Open)
                    {
                        try
                        {
                            FeedBackItem Item = (FeedBackItem)Para;
                            using (MySqlCommand Command1 = new MySqlCommand("Select Behavior From VoteRecordTable Where UserID=@UserID And GUID=@GUID", Connection))
                            {
                                _ = Command1.Parameters.AddWithValue("@UserID", SettingPage.ThisPage.UserID);
                                _ = Command1.Parameters.AddWithValue("@GUID", Item.GUID);

                                string Behaivor = Convert.ToString(Command1.ExecuteScalar());
                                if (!string.IsNullOrEmpty(Behaivor))
                                {
                                    Item.UserVoteAction = Behaivor;
                                }
                            }

                            return true;
                        }
                        catch (Exception)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            }, list);
        }

        public Task<bool> UpdateFeedBackVoteAsync(FeedBackItem Item)
        {
            return CreateConnectionToDataBaseAsync().ContinueWith((task) =>
            {
                using (MySqlConnection Connection = task.Result)
                {
                    if (Connection.State == System.Data.ConnectionState.Open)
                    {
                        try
                        {
                            using (MySqlCommand Command = new MySqlCommand("Update FeedBackTable Set LikeNum=@LikeNum, DislikeNum=@DislikeNum Where GUID=@GUID", Connection))
                            {
                                _ = Command.Parameters.AddWithValue("@LikeNum", Item.LikeNum);
                                _ = Command.Parameters.AddWithValue("@DislikeNum", Item.DislikeNum);
                                _ = Command.Parameters.AddWithValue("@GUID", Item.GUID);
                                _ = Command.ExecuteNonQuery();
                            }

                            using (MySqlCommand Command1 = new MySqlCommand("Select count(*) From VoteRecordTable Where UserID=@UserID And GUID=@GUID", Connection))
                            {
                                _ = Command1.Parameters.AddWithValue("@UserID", SettingPage.ThisPage.UserID);
                                _ = Command1.Parameters.AddWithValue("@GUID", Item.GUID);
                                if (Convert.ToInt16(Command1.ExecuteScalar()) == 0)
                                {
                                    if (Item.UserVoteAction != "=")
                                    {
                                        using (MySqlCommand Command = new MySqlCommand("Insert Into VoteRecordTable Values (@UserID,@GUID,@Behaivor)", Connection))
                                        {
                                            _ = Command.Parameters.AddWithValue("@UserID", SettingPage.ThisPage.UserID);
                                            _ = Command.Parameters.AddWithValue("@GUID", Item.GUID);
                                            _ = Command.Parameters.AddWithValue("@Behaivor", Item.UserVoteAction);
                                            _ = Command.ExecuteNonQuery();
                                        }
                                    }
                                }
                                else
                                {
                                    if (Item.UserVoteAction != "=")
                                    {
                                        using (MySqlCommand Command = new MySqlCommand("Update VoteRecordTable Set Behavior=@Behaivor Where UserID=@UserID And GUID=@GUID", Connection))
                                        {
                                            _ = Command.Parameters.AddWithValue("@UserID", SettingPage.ThisPage.UserID);
                                            _ = Command.Parameters.AddWithValue("@GUID", Item.GUID);
                                            _ = Command.Parameters.AddWithValue("@Behaivor", Item.UserVoteAction);
                                            _ = Command.ExecuteNonQuery();
                                        }
                                    }
                                    else
                                    {
                                        using (MySqlCommand Command = new MySqlCommand("Delete From VoteRecordTable Where UserID=@UserID And GUID=@GUID", Connection))
                                        {
                                            _ = Command.Parameters.AddWithValue("@UserID", SettingPage.ThisPage.UserID);
                                            _ = Command.Parameters.AddWithValue("@GUID", Item.GUID);
                                            _ = Command.ExecuteNonQuery();
                                        }
                                    }
                                }
                            }
                            return true;
                        }
                        catch (Exception)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            });
        }

        public Task<bool> UpdateFeedBackTitleAndSuggestionAsync(string Title, string Suggestion, string GUID)
        {
            return CreateConnectionToDataBaseAsync().ContinueWith((task) =>
            {
                using (MySqlConnection Connection = task.Result)
                {
                    if (Connection.State == System.Data.ConnectionState.Open)
                    {
                        try
                        {
                            using (MySqlCommand Command = new MySqlCommand("Update FeedBackTable Set Title=@NewTitle, Suggestion=@NewSuggestion Where GUID=@GUID", Connection))
                            {
                                _ = Command.Parameters.AddWithValue("@NewTitle", Title);
                                _ = Command.Parameters.AddWithValue("@NewSuggestion", Suggestion);
                                _ = Command.Parameters.AddWithValue("@GUID", GUID);
                                _ = Command.ExecuteNonQuery();
                            }
                            return true;
                        }
                        catch (Exception)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            });
        }

        public Task<bool> DeleteFeedBackAsync(FeedBackItem Item)
        {
            return CreateConnectionToDataBaseAsync().ContinueWith((task) =>
            {
                using (MySqlConnection Connection = task.Result)
                {
                    if (Connection.State == System.Data.ConnectionState.Open)
                    {
                        try
                        {
                            using (MySqlCommand Command = new MySqlCommand("Delete From FeedBackTable Where GUID=@GUID", Connection))
                            {
                                _ = Command.Parameters.AddWithValue("@GUID", Item.GUID);
                                _ = Command.ExecuteNonQuery();
                            }

                            using (MySqlCommand Command = new MySqlCommand("Delete From VoteRecordTable Where GUID=@GUID", Connection))
                            {
                                _ = Command.Parameters.AddWithValue("@GUID", Item.GUID);
                                _ = Command.ExecuteNonQuery();
                            }

                            return true;
                        }
                        catch (Exception)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            });
        }

        public Task<bool> SetFeedBackAsync(FeedBackItem Item)
        {
            return CreateConnectionToDataBaseAsync().ContinueWith((task) =>
            {
                using (MySqlConnection Connection = task.Result)
                {
                    if (Connection.State == System.Data.ConnectionState.Open)
                    {
                        try
                        {
                            using (MySqlCommand Command = new MySqlCommand("Insert Into FeedBackTable Values (@UserName,@Title,@Suggestion,@Like,@Dislike,@UserID,@GUID)", Connection))
                            {
                                _ = Command.Parameters.AddWithValue("@UserName", Item.UserName);
                                _ = Command.Parameters.AddWithValue("@Title", Item.Title);
                                _ = Command.Parameters.AddWithValue("@Suggestion", Item.Suggestion);
                                _ = Command.Parameters.AddWithValue("@Like", Item.LikeNum);
                                _ = Command.Parameters.AddWithValue("@Dislike", Item.DislikeNum);
                                _ = Command.Parameters.AddWithValue("@UserID", Item.UserID);
                                _ = Command.Parameters.AddWithValue("@GUID", Item.GUID);
                                _ = Command.ExecuteNonQuery();
                            }
                            return true;
                        }
                        catch (Exception)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            });
        }

        public void Dispose()
        {
            if (!IsDisposed)
            {
                IsDisposed = true;
                ConnectionLocker?.Dispose();
                ConnectionLocker = null;
                Instance = null;
            }
        }

        ~MySQL()
        {
            Dispose();
        }
    }
    #endregion

    #region 文件系统StorageFile类

    public enum ContentType
    {
        Folder = 0,
        File = 1
    }

    /// <summary>
    /// 提供对设备中的存储对象的描述
    /// </summary>
    public sealed class FileSystemStorageItem : INotifyPropertyChanged
    {
        /// <summary>
        /// 获取文件大小
        /// </summary>
        public string Size { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;
        /// <summary>
        /// 获取此文件的StorageFile对象
        /// </summary>
        public StorageFile File { get; private set; }

        public StorageFolder Folder { get; private set; }

        public ContentType ContentType { get; private set; }

        /// <summary>
        /// 获取此文件的缩略图
        /// </summary>
        public BitmapImage Thumbnail { get; private set; }

        /// <summary>
        /// 创建RemovableDeviceFile实例
        /// </summary>
        /// <param name="Size">文件大小</param>
        /// <param name="Item">文件StorageFile对象</param>
        /// <param name="Thumbnail">文件缩略图</param>
        public FileSystemStorageItem(IStorageItem Item, string Size, BitmapImage Thumbnail, string ModifiedTime)
        {
            if (Item.IsOfType(StorageItemTypes.File))
            {
                File = Item as StorageFile;
                ContentType = ContentType.File;
            }
            else if (Item.IsOfType(StorageItemTypes.Folder))
            {
                Folder = Item as StorageFolder;
                ContentType = ContentType.Folder;
            }
            else
            {
                throw new Exception("Item must be folder or file");
            }
            this.Size = Size;
            this.Thumbnail = Thumbnail;
            this.ModifiedTime = ModifiedTime;
        }

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        /// <summary>
        /// 更新文件以及文件大小，并通知UI界面
        /// </summary>
        /// <param name="File"></param>
        public async Task UpdateRequested(IStorageItem Item)
        {
            if (Item is StorageFolder Folder && ContentType == ContentType.Folder)
            {
                this.Folder = Folder;
            }
            else if (Item is StorageFile File && ContentType == ContentType.File)
            {
                this.File = File;
            }
            else
            {
                throw new Exception("Unsupport IStorageItem Or IStorageItem does not match the RemovableDeviceFile");
            }

            Size = await Item.GetSizeDescriptionAsync();
            OnPropertyChanged("DisplayName");
            OnPropertyChanged("Size");
        }

        /// <summary>
        /// 更新文件大小，并通知UI界面
        /// </summary>
        public async Task SizeUpdateRequested()
        {
            switch (ContentType)
            {
                case ContentType.File:
                    Size = await File.GetSizeDescriptionAsync();
                    break;
                case ContentType.Folder:
                    throw new Exception("Could not update folder size");
            }
            OnPropertyChanged("Size");
        }

        /// <summary>
        /// 获取文件的文件名(不包含后缀)
        /// </summary>
        public string DisplayName
        {
            get
            {
                return ContentType == ContentType.Folder ?
                    (string.IsNullOrEmpty(Folder.DisplayName) ? Folder.Name : Folder.DisplayName) :
                    (string.IsNullOrEmpty(File.DisplayName) ? File.Name : (File.DisplayName.EndsWith(File.FileType) ? File.DisplayName.Remove(File.DisplayName.LastIndexOf(".")) : File.DisplayName));
            }
        }

        public string ModifiedTime { get; private set; }

        public string Path
        {
            get
            {
                return ContentType == ContentType.Folder ? Folder.Path : File.Path;
            }
        }

        /// <summary>
        /// 获取文件的完整文件名(包括后缀)
        /// </summary>
        public string Name
        {
            get
            {
                return ContentType == ContentType.Folder ? Folder.Name : File.Name;
            }
        }

        /// <summary>
        /// 获取文件类型描述
        /// </summary>
        public string DisplayType
        {
            get
            {
                return ContentType == ContentType.Folder ? Folder.DisplayType : File.DisplayType;
            }
        }

        /// <summary>
        /// 获取文件的类型
        /// </summary>
        public string Type
        {
            get
            {
                return ContentType == ContentType.Folder ? Folder.DisplayType : File.FileType;
            }
        }

        /// <summary>
        /// 获取文件唯一标识符
        /// </summary>
        public string RelativeId
        {
            get
            {
                return ContentType == ContentType.Folder ? Folder.FolderRelativeId : File.FolderRelativeId;
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }
    #endregion

    #region Zip文件查看器显示类
    /// <summary>
    /// 提供Zip内部文件的显示
    /// </summary>
    public sealed class ZipFileDisplay
    {
        /// <summary>
        /// 获取文件名
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// 获取压缩后的大小
        /// </summary>
        public string CompresionSize { get; private set; }

        /// <summary>
        /// 获取文件实际大小
        /// </summary>
        public string ActualSize { get; private set; }

        /// <summary>
        /// 获取文件修改时间
        /// </summary>
        public string Time { get; private set; }

        /// <summary>
        /// 获取文件类型
        /// </summary>
        public string Type { get; private set; }

        /// <summary>
        /// 获取是否加密的描述
        /// </summary>
        public string IsCrypted { get; private set; }

        public string FullName { get; private set; }

        /// <summary>
        /// 创建ZipFileDisplay的实例
        /// </summary>
        public ZipFileDisplay(ZipEntry Entry)
        {
            FullName = Entry.Name;

            if (Globalization.Language == LanguageEnum.Chinese)
            {
                CompresionSize = "压缩大小：" + GetSize(Entry.CompressedSize);
                ActualSize = "解压大小：" + GetSize(Entry.Size);
                Time = "创建时间：" + Entry.DateTime.ToString("F");

                int index = FullName.LastIndexOf(".");
                if (index != -1)
                {
                    Name = Path.GetFileNameWithoutExtension(FullName);
                    Type = Path.GetExtension(FullName).Substring(1).ToUpper() + "文件";
                }
                else
                {
                    Name = FullName;
                    Type = "未知文件类型";
                }

                if (Entry.IsCrypted)
                {
                    IsCrypted = "密码保护：是";
                }
                else
                {
                    IsCrypted = "密码保护：否";
                }
            }
            else
            {
                CompresionSize = "Compressed：" + GetSize(Entry.CompressedSize);
                ActualSize = "ActualSize：" + GetSize(Entry.Size);
                Time = "Created：" + Entry.DateTime.ToString("F");

                int index = FullName.LastIndexOf(".");
                if (index != -1)
                {
                    Name = Path.GetFileNameWithoutExtension(FullName);
                    Type = Path.GetExtension(FullName).Substring(1).ToUpper() + "File";
                }
                else
                {
                    Name = FullName;
                    Type = "Unknown Type";
                }

                if (Entry.IsCrypted)
                {
                    IsCrypted = "Encrypted：True";
                }
                else
                {
                    IsCrypted = "Encrypted：False";
                }
            }
        }

        /// <summary>
        /// 获取文件大小的描述
        /// </summary>
        /// <param name="Size">大小</param>
        /// <returns>大小描述</returns>
        private string GetSize(long Size)
        {
            return Size / 1024f < 1024 ? Math.Round(Size / 1024f, 2).ToString() + " KB" :
            (Size / 1048576f >= 1024 ? Math.Round(Size / 1073741824f, 2).ToString() + " GB" :
            Math.Round(Size / 1048576f, 2).ToString() + " MB");
        }
    }
    #endregion

    #region Zip相关枚举
    /// <summary>
    /// AES加密密钥长度枚举
    /// </summary>
    public enum KeySize
    {
        /// <summary>
        /// 无
        /// </summary>
        None = 0,

        /// <summary>
        /// AES-128bit
        /// </summary>
        AES128 = 128,

        /// <summary>
        /// AES-256bit
        /// </summary>
        AES256 = 256
    }

    /// <summary>
    /// 压缩等级枚举
    /// </summary>
    public enum CompressionLevel
    {
        /// <summary>
        /// 最大
        /// </summary>
        Max = 9,

        /// <summary>
        /// 高于标准
        /// </summary>
        AboveStandard = 7,

        /// <summary>
        /// 标准
        /// </summary>
        Standard = 5,

        /// <summary>
        /// 低于标准
        /// </summary>
        BelowStandard = 3,

        /// <summary>
        /// 仅打包
        /// </summary>
        PackOnly = 1
    }
    #endregion

    #region Zip加密界面绑定转换器
    public sealed class ZipCryptConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (!(value is bool))
            {
                return null;
            }

            var IsEnable = (bool)value;
            if (IsEnable)
            {
                return Visibility.Visible;
            }
            else
            {
                return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
    #endregion

    #region 图片展示类
    /// <summary>
    /// 为图片查看提供支持
    /// </summary>
    public sealed class PhotoDisplaySupport : INotifyPropertyChanged
    {
        /// <summary>
        /// 获取Bitmap图片对象
        /// </summary>
        public BitmapImage BitmapSource { get; private set; }

        /// <summary>
        /// 获取Photo文件名称
        /// </summary>
        public string FileName
        {
            get
            {
                return PhotoFile.Name;
            }
        }

        private bool IsThumbnailPicture = true;

        public int RotateAngle { get; set; } = 0;

        /// <summary>
        /// 获取Photo的StorageFile对象
        /// </summary>
        public StorageFile PhotoFile { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public PhotoDisplaySupport(BitmapImage ImageSource, StorageFile File)
        {
            BitmapSource = ImageSource;
            PhotoFile = File;
        }

        public async Task ReplaceThumbnailBitmap()
        {
            if (IsThumbnailPicture)
            {
                IsThumbnailPicture = false;
                using (var Stream = await PhotoFile.OpenAsync(FileAccessMode.Read))
                {
                    await BitmapSource.SetSourceAsync(Stream);
                }
                OnPropertyChanged("BitmapSource");
            }
        }

        public async Task UpdateImage()
        {
            using (var Stream = await PhotoFile.OpenAsync(FileAccessMode.Read))
            {
                await BitmapSource.SetSourceAsync(Stream);
            }
            OnPropertyChanged("BitmapSource");
        }

        public async Task<SoftwareBitmap> GenerateImageWithRotation()
        {
            using (var stream = await PhotoFile.OpenAsync(FileAccessMode.Read))
            {
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);

                switch (RotateAngle % 360)
                {
                    case 0:
                        {
                            return await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
                        }
                    case 90:
                        {
                            using (var Origin = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied))
                            {
                                SoftwareBitmap Processed = new SoftwareBitmap(BitmapPixelFormat.Bgra8, Origin.PixelHeight, Origin.PixelWidth, BitmapAlphaMode.Premultiplied);
                                OpenCV.OpenCVLibrary.RotateEffect(Origin, Processed, 90);
                                return Processed;
                            }
                        }
                    case 180:
                        {
                            var Origin = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
                            OpenCV.OpenCVLibrary.RotateEffect(Origin, Origin, 180);
                            return Origin;
                        }
                    case 270:
                        {
                            using (var Origin = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied))
                            {
                                SoftwareBitmap Processed = new SoftwareBitmap(BitmapPixelFormat.Bgra8, Origin.PixelHeight, Origin.PixelWidth, BitmapAlphaMode.Premultiplied);
                                OpenCV.OpenCVLibrary.RotateEffect(Origin, Processed, -90);
                                return Processed;
                            }
                        }
                    default:
                        {
                            return null;
                        }
                }
            }
        }

        private void OnPropertyChanged(string Name)
        {
            if (!string.IsNullOrEmpty(Name))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(Name));
            }
        }
    }
    #endregion

    #region 图片查看器相关
    public enum FilterType
    {
        Origin = 0,
        Invert = 1,
        Gray = 2,
        Threshold = 4,
        Sketch = 8,
        GaussianBlur = 16,
        Sepia = 32,
        Mosaic = 64,
        OilPainting = 128
    }

    public sealed class FilterItem : IDisposable
    {
        public string Text { get; private set; }

        public FilterType Type { get; private set; }

        public SoftwareBitmapSource Bitmap { get; private set; }

        private bool IsDisposed = false;

        public FilterItem(SoftwareBitmapSource Bitmap, string Text, FilterType Type)
        {
            this.Bitmap = Bitmap;
            this.Text = Text;
            this.Type = Type;
        }

        public void Dispose()
        {
            if (!IsDisposed)
            {
                IsDisposed = true;
                Bitmap.Dispose();
            }
        }
    }

    public sealed class AlphaSliderValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            int Value = System.Convert.ToInt32(((double)value - 1) * 100);
            return Value > 0 ? ("+" + Value) : Value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class BetaSliderValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            int Value = System.Convert.ToInt32(value);
            return Value > 0 ? ("+" + Value) : Value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
    #endregion

    #region Zip自定义静态数据源
    public sealed class CustomStaticDataSource : IStaticDataSource
    {
        private Stream stream;

        public Stream GetSource()
        {
            return stream;
        }

        public void SetStream(Stream inputStream)
        {
            stream = inputStream;
            stream.Position = 0;
        }
    }
    #endregion

    #region lock关键字同步锁全局对象提供器
    /// <summary>
    /// 提供全局锁定根
    /// </summary>
    public class SyncRootProvider
    {
        /// <summary>
        /// 锁定根对象
        /// </summary>
        public static object SyncRoot { get; } = new object();
    }
    #endregion

    #region 文件夹图标状态更改转换器
    public sealed class FolderStateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (!(value is bool))
            {
                return null;
            }

            if ((bool)value)
            {
                return "\xE838";
            }
            else
            {
                return "\xED41";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
    #endregion

    #region 文件大小显示状态更改转换器
    public sealed class SizeDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is StorageFolder)
            {
                return Visibility.Collapsed;
            }
            else
            {
                return Visibility.Visible;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
    #endregion

    #region Json翻译对象
    public class TranslationResult
    {
        public DetectedLanguage DetectedLanguage { get; set; }
        public TextResult SourceText { get; set; }
        public Translation[] Translations { get; set; }
    }

    public class DetectedLanguage
    {
        public string Language { get; set; }
        public float Score { get; set; }
    }

    public class TextResult
    {
        public string Text { get; set; }
        public string Script { get; set; }
    }

    public class Translation
    {
        public string Text { get; set; }
        public TextResult Transliteration { get; set; }
        public string To { get; set; }
        public Alignment Alignment { get; set; }
        public SentenceLength SentLen { get; set; }
    }

    public class Alignment
    {
        public string Proj { get; set; }
    }

    public class SentenceLength
    {
        public int[] SrcSentLen { get; set; }
        public int[] TransSentLen { get; set; }
    }
    #endregion

    #region 扩展方法类
    public static class Extention
    {
        public static async Task SelectNode(this TreeViewNode Node, TreeView View)
        {
            if (View == null)
            {
                throw new ArgumentException("View could not be null");
            }

            while (true)
            {
                if (View.ContainerFromNode(Node) is TreeViewItem Item)
                {
                    Item.IsSelected = true;
                    Item.StartBringIntoView(new BringIntoViewOptions { AnimationDesired = true, VerticalAlignmentRatio = 0.5 });
                    break;
                }
                else
                {
                    await Task.Delay(200);
                }
            }
        }

        public static async Task<bool> CheckExist(this StorageFile File)
        {
            try
            {
                if ((await File.GetParentAsync()) is StorageFolder ParentFolder)
                {
                    return (await ParentFolder.TryGetItemAsync(File.Name)) != null;
                }
                else
                {
                    try
                    {
                        _ = await StorageFile.GetFileFromPathAsync(File.Path);
                        return true;
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                }
            }
            catch (Exception)
            {
                try
                {
                    _ = await StorageFile.GetFileFromPathAsync(File.Path);
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        public static async Task<bool> CheckExist(this StorageFolder Folder)
        {
            try
            {
                if ((await Folder.GetParentAsync()) is StorageFolder ParenetFolder)
                {
                    return (await ParenetFolder.TryGetItemAsync(Folder.Name)) != null;
                }
                else
                {
                    try
                    {
                        _ = await StorageFolder.GetFolderFromPathAsync(Folder.Path);
                        return true;
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                }
            }
            catch (Exception)
            {
                try
                {
                    _ = await StorageFolder.GetFolderFromPathAsync(Folder.Path);
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        public static async Task<string> EncryptAsync(this string OriginText, string Key)
        {
            try
            {
                using (AesCryptoServiceProvider AES = new AesCryptoServiceProvider
                {
                    KeySize = 128,
                    Key = Key.Length > 16 ? Encoding.UTF8.GetBytes(Key.Substring(0, 16)) : Encoding.UTF8.GetBytes(Key.PadRight(16, '0')),
                    Mode = CipherMode.CBC,
                    Padding = PaddingMode.PKCS7,
                    IV = Encoding.UTF8.GetBytes("KUsaWlEy2XN5b6y8")
                })
                {
                    using (MemoryStream EncryptStream = new MemoryStream())
                    {
                        using (ICryptoTransform Encryptor = AES.CreateEncryptor())
                        using (CryptoStream TransformStream = new CryptoStream(EncryptStream, Encryptor, CryptoStreamMode.Write))
                        {
                            using (StreamWriter Writer = new StreamWriter(TransformStream))
                            {
                                await Writer.WriteAsync(OriginText);
                            }
                        }

                        return Convert.ToBase64String(EncryptStream.ToArray());
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static async Task<string> DecryptAsync(this string OriginText, string Key)
        {
            try
            {
                using (AesCryptoServiceProvider AES = new AesCryptoServiceProvider
                {
                    KeySize = 128,
                    Key = Key.Length > 16 ? Encoding.UTF8.GetBytes(Key.Substring(0, 16)) : Encoding.UTF8.GetBytes(Key.PadRight(16, '0')),
                    Mode = CipherMode.CBC,
                    Padding = PaddingMode.PKCS7,
                    IV = Encoding.UTF8.GetBytes("KUsaWlEy2XN5b6y8")
                })
                {
                    using (MemoryStream DecryptStream = new MemoryStream(Convert.FromBase64String(OriginText)))
                    {
                        using (ICryptoTransform Decryptor = AES.CreateDecryptor())
                        using (CryptoStream TransformStream = new CryptoStream(DecryptStream, Decryptor, CryptoStreamMode.Read))
                        using (StreamReader Writer = new StreamReader(TransformStream, Encoding.UTF8))
                        {
                            return await Writer.ReadToEndAsync();
                        }
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static async Task<StorageFile> EncryptAsync(this StorageFile OriginFile, StorageFolder ExportFolder, string Key, int KeySize)
        {
            if (ExportFolder == null)
            {
                throw new ArgumentException("ExportFolder could not be null");
            }

            if (KeySize != 256 && KeySize != 128)
            {
                throw new InvalidEnumArgumentException("AES密钥长度仅支持128或256任意一种");
            }

            byte[] KeyArray = null;

            int KeyLengthNeed = KeySize / 8;

            KeyArray = Key.Length > KeyLengthNeed
                       ? Encoding.UTF8.GetBytes(Key.Substring(0, KeyLengthNeed))
                       : Encoding.UTF8.GetBytes(Key.PadRight(KeyLengthNeed, '0'));

            StorageFile EncryptedFile = null;
            try
            {
                EncryptedFile = await ExportFolder.CreateFileAsync($"{ Path.GetFileNameWithoutExtension(OriginFile.Name)}.sle", CreationCollisionOption.GenerateUniqueName);

                using (AesCryptoServiceProvider AES = new AesCryptoServiceProvider
                {
                    KeySize = KeySize,
                    Key = KeyArray,
                    Mode = CipherMode.CBC,
                    Padding = PaddingMode.Zeros,
                    IV = Encoding.UTF8.GetBytes("HqVQ2YgUnUlRNp5Z")
                })
                {
                    using (Stream OriginFileStream = await OriginFile.OpenStreamForReadAsync())
                    using (Stream EncryptFileStream = await EncryptedFile.OpenStreamForWriteAsync())
                    using (ICryptoTransform Encryptor = AES.CreateEncryptor())
                    {
                        byte[] Detail = Encoding.UTF8.GetBytes("$" + KeySize + "|" + OriginFile.FileType + "$");
                        await EncryptFileStream.WriteAsync(Detail, 0, Detail.Length);

                        byte[] PasswordFlag = Encoding.UTF8.GetBytes("PASSWORD_CORRECT");
                        byte[] EncryptPasswordFlag = Encryptor.TransformFinalBlock(PasswordFlag, 0, PasswordFlag.Length);
                        await EncryptFileStream.WriteAsync(EncryptPasswordFlag, 0, EncryptPasswordFlag.Length);

                        using (CryptoStream TransformStream = new CryptoStream(EncryptFileStream, Encryptor, CryptoStreamMode.Write))
                        {
                            await OriginFileStream.CopyToAsync(TransformStream);
                            TransformStream.FlushFinalBlock();
                        }
                    }
                }

                return EncryptedFile;
            }
            catch (Exception e)
            {
                await EncryptedFile?.DeleteAsync(StorageDeleteOption.PermanentDelete);
                throw e;
            }
        }

        public static async Task<StorageFile> DecryptAsync(this StorageFile EncryptedFile, StorageFolder ExportFolder, string Key)
        {
            if (ExportFolder == null)
            {
                throw new ArgumentException("ExportFolder could not be null");
            }

            StorageFile DecryptedFile = null;
            try
            {
                using (AesCryptoServiceProvider AES = new AesCryptoServiceProvider
                {
                    Mode = CipherMode.CBC,
                    Padding = PaddingMode.Zeros,
                    IV = Encoding.UTF8.GetBytes("HqVQ2YgUnUlRNp5Z")
                })
                {
                    using (Stream EncryptFileStream = await EncryptedFile.OpenStreamForReadAsync())
                    {
                        byte[] DecryptByteBuffer = new byte[20];

                        await EncryptFileStream.ReadAsync(DecryptByteBuffer, 0, DecryptByteBuffer.Length);

                        string FileType;
                        if (Encoding.UTF8.GetString(DecryptByteBuffer).Split('$', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() is string Info)
                        {
                            string[] InfoGroup = Info.Split('|');
                            if (InfoGroup.Length == 2)
                            {
                                int KeySize = Convert.ToInt32(InfoGroup[0]);
                                FileType = InfoGroup[1];

                                AES.KeySize = KeySize;

                                int KeyLengthNeed = KeySize / 8;
                                AES.Key = Key.Length > KeyLengthNeed ? Encoding.UTF8.GetBytes(Key.Substring(0, KeyLengthNeed)) : Encoding.UTF8.GetBytes(Key.PadRight(KeyLengthNeed, '0'));
                            }
                            else
                            {
                                throw new FileDamagedException("文件损坏，无法解密");
                            }
                        }
                        else
                        {
                            throw new FileDamagedException("文件损坏，无法解密");
                        }

                        DecryptedFile = await ExportFolder.CreateFileAsync($"{ Path.GetFileNameWithoutExtension(EncryptedFile.Name)}{FileType}", CreationCollisionOption.GenerateUniqueName);

                        using (Stream DecryptFileStream = await DecryptedFile.OpenStreamForWriteAsync())
                        using (ICryptoTransform Decryptor = AES.CreateDecryptor(AES.Key, AES.IV))
                        {
                            byte[] PasswordConfirm = new byte[16];
                            EncryptFileStream.Seek(Info.Length + 2, SeekOrigin.Begin);
                            await EncryptFileStream.ReadAsync(PasswordConfirm, 0, PasswordConfirm.Length);

                            if (Encoding.UTF8.GetString(Decryptor.TransformFinalBlock(PasswordConfirm, 0, PasswordConfirm.Length)) == "PASSWORD_CORRECT")
                            {
                                using (CryptoStream TransformStream = new CryptoStream(DecryptFileStream, Decryptor, CryptoStreamMode.Write))
                                {
                                    await EncryptFileStream.CopyToAsync(TransformStream);
                                    TransformStream.FlushFinalBlock();
                                }
                            }
                            else
                            {
                                throw new PasswordErrorException("密码错误");
                            }
                        }

                        return DecryptedFile;
                    }
                }
            }
            catch (Exception e)
            {
                await DecryptedFile?.DeleteAsync(StorageDeleteOption.PermanentDelete);
                throw e;
            }
        }

        public static string TranslateToPinyinOrStayInEnglish(this string From)
        {
            StringBuilder EnglishBuilder = new StringBuilder();
            StringBuilder ChineseBuilder = new StringBuilder();
            foreach (var Char in From)
            {
                if (PinyinHelper.IsChinese(Char))
                {
                    _ = ChineseBuilder.Append(Char);
                }
                else
                {
                    _ = EnglishBuilder.Append(Char);
                }
            }

            if (ChineseBuilder.Length != 0 && EnglishBuilder.Length != 0)
            {
                return EnglishBuilder.ToString() + " " + PinyinHelper.GetPinyin(ChineseBuilder.ToString());
            }
            else if (ChineseBuilder.Length == 0 && EnglishBuilder.Length != 0)
            {
                return EnglishBuilder.ToString();
            }
            else if (ChineseBuilder.Length != 0 && EnglishBuilder.Length == 0)
            {
                return PinyinHelper.GetPinyin(ChineseBuilder.ToString());
            }
            else
            {
                return string.Empty;
            }
        }

        public static async Task<string> TranslateToAsync(this string From, LanguageEnum language)
        {
            using (HttpClient Client = new HttpClient())
            using (HttpRequestMessage Request = new HttpRequestMessage())
            {

                Request.Method = HttpMethod.Post;
                Request.RequestUri = new Uri($"https://api.cognitive.microsofttranslator.com/translate?api-version=3.0&to={(language == LanguageEnum.Chinese ? "zh-Hans" : "en")}");
                Request.Content = new StringContent(JsonConvert.SerializeObject(new object[] { new { Text = From } }), Encoding.UTF8, "application/json");
                Request.Headers.Add("Ocp-Apim-Subscription-Key", "3e0230e26b134d7ab9ffdc566deadf9c");

                try
                {
                    HttpResponseMessage Response = await Client.SendAsync(Request);
                    string result = await Response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(result))
                    {
                        TranslationResult[] deserializedOutput = JsonConvert.DeserializeObject<TranslationResult[]>(result);

                        if (deserializedOutput.FirstOrDefault() is TranslationResult Result)
                        {
                            if (Result.DetectedLanguage.Language.StartsWith("en") && language == LanguageEnum.English)
                            {
                                return From;
                            }
                            else if (Result.DetectedLanguage.Language.StartsWith("zh") && language == LanguageEnum.Chinese)
                            {
                                return From;
                            }
                            else
                            {
                                return Result.Translations.FirstOrDefault()?.Text;
                            }
                        }
                        else
                        {
                            return null;
                        }
                    }
                    else
                    {
                        return null;
                    }
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        public static string TranslateTo(this string From, LanguageEnum language)
        {
            using (HttpClient Client = new HttpClient())
            using (HttpRequestMessage Request = new HttpRequestMessage())
            {

                Request.Method = HttpMethod.Post;
                Request.RequestUri = new Uri($"https://api.cognitive.microsofttranslator.com/translate?api-version=3.0&to={(language == LanguageEnum.Chinese ? "zh-Hans" : "en")}");
                Request.Content = new StringContent(JsonConvert.SerializeObject(new object[] { new { Text = From } }), Encoding.UTF8, "application/json");
                Request.Headers.Add("Ocp-Apim-Subscription-Key", "3e0230e26b134d7ab9ffdc566deadf9c");

                try
                {
                    HttpResponseMessage Response = Client.SendAsync(Request).Result;
                    string result = Response.Content.ReadAsStringAsync().Result;
                    if (!string.IsNullOrEmpty(result))
                    {
                        TranslationResult[] deserializedOutput = JsonConvert.DeserializeObject<TranslationResult[]>(result);

                        if (deserializedOutput.FirstOrDefault() is TranslationResult Result)
                        {
                            if (Result.DetectedLanguage.Language.StartsWith("en") && language == LanguageEnum.English)
                            {
                                return From;
                            }
                            else if (Result.DetectedLanguage.Language.StartsWith("zh") && language == LanguageEnum.Chinese)
                            {
                                return From;
                            }
                            else
                            {
                                return Result.Translations.FirstOrDefault()?.Text;
                            }
                        }
                        else
                        {
                            return null;
                        }
                    }
                    else
                    {
                        return null;
                    }
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        public static T FindChildOfType<T>(this DependencyObject root) where T : DependencyObject
        {
            Queue<DependencyObject> ObjectQueue = new Queue<DependencyObject>();
            ObjectQueue.Enqueue(root);
            while (ObjectQueue.Count > 0)
            {
                DependencyObject Current = ObjectQueue.Dequeue();
                if (Current != null)
                {
                    for (int i = 0; i < VisualTreeHelper.GetChildrenCount(Current); i++)
                    {
                        var ChildObject = VisualTreeHelper.GetChild(Current, i);
                        if (ChildObject is T TypedChild)
                        {
                            return TypedChild;
                        }
                        ObjectQueue.Enqueue(ChildObject);
                    }
                }
            }
            return null;
        }

        public static T FindChildOfName<T>(this DependencyObject root, string name) where T : DependencyObject
        {
            Queue<DependencyObject> ObjectQueue = new Queue<DependencyObject>();
            ObjectQueue.Enqueue(root);
            while (ObjectQueue.Count > 0)
            {
                DependencyObject Current = ObjectQueue.Dequeue();
                if (Current != null)
                {
                    for (int i = 0; i < VisualTreeHelper.GetChildrenCount(Current); i++)
                    {
                        var ChildObject = VisualTreeHelper.GetChild(Current, i);
                        if (ChildObject is T TypedChild && (TypedChild as FrameworkElement).Name == name)
                        {
                            return TypedChild;
                        }
                        ObjectQueue.Enqueue(ChildObject);
                    }
                }
            }
            return null;
        }

        public static async Task UpdateAllSubNodeFolder(this TreeViewNode ParentNode)
        {
            StorageFolder ParentFolder = ParentNode.Content as StorageFolder;
            foreach (var Package in ParentNode.Children.Select((SubNode) => new { (SubNode.Content as StorageFolder).Name, SubNode }))
            {
                Package.SubNode.Content = await ParentFolder.GetFolderAsync(Package.Name);

                if (Package.SubNode.HasChildren)
                {
                    await UpdateAllSubNodeFolder(Package.SubNode);
                }
            }
        }

        public static async Task DeleteAllSubFilesAndFolders(this StorageFolder Folder)
        {
            foreach (var Item in await Folder.GetItemsAsync())
            {
                if (Item is StorageFolder folder)
                {
                    await DeleteAllSubFilesAndFolders(folder);
                }
                else
                {
                    await Item.DeleteAsync(StorageDeleteOption.PermanentDelete);
                }
            }
        }

        public static async Task<string> GetSizeDescriptionAsync(this IStorageItem Item)
        {
            BasicProperties Properties = await Item.GetBasicPropertiesAsync();
            return Properties.Size / 1024f < 1024 ? Math.Round(Properties.Size / 1024f, 2).ToString("0.00") + " KB" :
            (Properties.Size / 1048576f < 1024 ? Math.Round(Properties.Size / 1048576f, 2).ToString("0.00") + " MB" :
            (Properties.Size / 1073741824f < 1024 ? Math.Round(Properties.Size / 1073741824f, 2).ToString("0.00") + " GB" :
            Math.Round(Properties.Size / Convert.ToDouble(1099511627776), 2).ToString() + " TB"));
        }

        public static async Task<string> GetModifiedTimeAsync(this IStorageItem Item)
        {
            var Properties = await Item.GetBasicPropertiesAsync();
            return $"{Properties.DateModified.Year}年{Properties.DateModified.Month}月{Properties.DateModified.Day}日, {(Properties.DateModified.Hour < 10 ? "0" + Properties.DateModified.Hour : Properties.DateModified.Hour.ToString())}:{(Properties.DateModified.Minute < 10 ? "0" + Properties.DateModified.Minute : Properties.DateModified.Minute.ToString())}:{(Properties.DateModified.Second < 10 ? "0" + Properties.DateModified.Second : Properties.DateModified.Second.ToString())}";
        }

        public static async Task<BitmapImage> GetThumbnailBitmapAsync(this IStorageItem Item)
        {
            try
            {
                if (Item is StorageFolder Folder)
                {
                    using (StorageItemThumbnail Thumbnail = await Folder.GetScaledImageAsThumbnailAsync(ThumbnailMode.ListView, 100))
                    {
                        if (Thumbnail == null || Thumbnail.Size == 0 || Thumbnail.OriginalHeight == 0 || Thumbnail.OriginalWidth == 0)
                        {
                            return null;
                        }

                        BitmapImage bitmapImage = new BitmapImage
                        {
                            DecodePixelHeight = 100,
                            DecodePixelWidth = 100
                        };
                        await bitmapImage.SetSourceAsync(Thumbnail);
                        return bitmapImage;
                    }
                }
                else if (Item is StorageFile File)
                {
                    using (StorageItemThumbnail Thumbnail = await File.GetScaledImageAsThumbnailAsync(ThumbnailMode.ListView, 100))
                    {
                        if (Thumbnail == null)
                        {
                            return null;
                        }

                        BitmapImage bitmapImage = new BitmapImage
                        {
                            DecodePixelHeight = 100,
                            DecodePixelWidth = 100
                        };
                        await bitmapImage.SetSourceAsync(Thumbnail);
                        return bitmapImage;
                    }
                }
                else
                {
                    return null;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static void ScrollIntoViewSmoothly(this ListViewBase listViewBase, object item, ScrollIntoViewAlignment alignment = ScrollIntoViewAlignment.Default)
        {
            if (listViewBase.FindChildOfType<ScrollViewer>() is ScrollViewer scrollViewer)
            {
                double originHorizontalOffset = scrollViewer.HorizontalOffset;
                double originVerticalOffset = scrollViewer.VerticalOffset;

                void layoutUpdatedHandler(object sender, object e)
                {
                    listViewBase.LayoutUpdated -= layoutUpdatedHandler;

                    double targetHorizontalOffset = scrollViewer.HorizontalOffset;
                    double targetVerticalOffset = scrollViewer.VerticalOffset;

                    void scrollHandler(object s, ScrollViewerViewChangedEventArgs t)
                    {
                        scrollViewer.ViewChanged -= scrollHandler;

                        scrollViewer.ChangeView(targetHorizontalOffset, targetVerticalOffset, null);
                    }

                    scrollViewer.ViewChanged += scrollHandler;

                    scrollViewer.ChangeView(originHorizontalOffset, originVerticalOffset, null, true);
                }

                listViewBase.LayoutUpdated += layoutUpdatedHandler;

                listViewBase.ScrollIntoView(item, alignment);
            }
            else
            {
                listViewBase.ScrollIntoView(item, alignment);
            }
        }
    }
    #endregion

    #region 蓝牙设备列表类
    /// <summary>
    /// 为蓝牙模块提供蓝牙设备信息保存功能
    /// </summary>
    public sealed class BluetoothList : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 表示蓝牙设备
        /// </summary>
        public DeviceInformation DeviceInfo { get; set; }
        public void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        /// <summary>
        /// 获取蓝牙设备名称
        /// </summary>
        public string Name
        {
            get
            {
                return string.IsNullOrWhiteSpace(DeviceInfo.Name) ? (Globalization.Language == LanguageEnum.Chinese ? "未知设备" : "Unknown") : DeviceInfo.Name;
            }
        }

        public BitmapImage Glyph { get; private set; }

        /// <summary>
        /// 获取蓝牙标识字符串
        /// </summary>
        public string Id
        {
            get
            {
                return DeviceInfo.Id;
            }
        }

        /// <summary>
        /// 获取配对情况描述字符串
        /// </summary>
        public string IsPaired
        {
            get
            {
                if (DeviceInfo.Pairing.IsPaired)
                {
                    return Globalization.Language == LanguageEnum.Chinese ? "已配对" : "Paired";
                }
                else
                {
                    return Globalization.Language == LanguageEnum.Chinese ? "准备配对" : "ReadyToPair";
                }
            }
        }

        /// <summary>
        /// Button显示属性
        /// </summary>
        public string CancelOrPairButton
        {
            get
            {
                if (DeviceInfo.Pairing.IsPaired)
                {
                    return Globalization.Language == LanguageEnum.Chinese ? "取消配对" : "Unpair";
                }
                else
                {
                    return Globalization.Language == LanguageEnum.Chinese ? "配对" : "Pair";
                }
            }
        }

        /// <summary>
        /// 更新蓝牙设备信息
        /// </summary>
        /// <param name="DeviceInfoUpdate">蓝牙设备的更新属性</param>
        public void Update(DeviceInformationUpdate DeviceInfoUpdate)
        {
            DeviceInfo.Update(DeviceInfoUpdate);
            OnPropertyChanged("IsPaired");
            OnPropertyChanged("Name");
        }

        /// <summary>
        /// 创建BluetoothList的实例
        /// </summary>
        /// <param name="DeviceInfo">蓝牙设备</param>
        public BluetoothList(DeviceInformation DeviceInfo, BitmapImage Glyph)
        {
            this.DeviceInfo = DeviceInfo;
            this.Glyph = Glyph;
        }
    }
    #endregion

    #region 蓝牙Obex协议对象类
    /// <summary>
    /// 提供蓝牙OBEX协议服务
    /// </summary>
    public sealed class ObexServiceProvider
    {
        /// <summary>
        /// 蓝牙设备
        /// </summary>
        private static BluetoothDevice BTDevice;

        /// <summary>
        /// OBEX协议服务
        /// </summary>
        public static ObexService GetObexNewInstance()
        {
            if (BTDevice != null)
            {
                return ObexService.GetDefaultForBluetoothDevice(BTDevice);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 设置Obex对象的实例
        /// </summary>
        /// <param name="obex">OBEX对象</param>
        public static void SetObexInstance(BluetoothDevice BT)
        {
            BTDevice = BT;
        }
    }
    #endregion

    #region 文件路径逐层解析类
    public sealed class PathAnalysis
    {
        public string FullPath { get; private set; }

        private Queue<string> PathQueue;

        private string CurrentLevel;

        public PathAnalysis(string FullPath, string CurrentPath)
        {
            this.FullPath = FullPath;

            CurrentLevel = CurrentPath;

            if (string.IsNullOrEmpty(CurrentPath))
            {
                string[] Split = FullPath.Split("\\", StringSplitOptions.RemoveEmptyEntries);
                Split[0] = Split[0] + "\\";
                PathQueue = new Queue<string>(Split);
            }
            else
            {
                if (FullPath != CurrentPath)
                {
                    string[] Split = Path.GetRelativePath(CurrentPath, FullPath).Split("\\", StringSplitOptions.RemoveEmptyEntries);
                    PathQueue = new Queue<string>(Split);
                }
                else
                {
                    PathQueue = new Queue<string>(0);
                }
            }
        }

        public string NextPathLevel()
        {
            if (PathQueue.Count != 0)
            {
                return CurrentLevel = Path.Combine(CurrentLevel, PathQueue.Dequeue());
            }
            else
            {
                return CurrentLevel;
            }
        }
    }
    #endregion

    #region 这台电脑相关驱动器和库显示类
    public sealed class HardDeviceInfo
    {
        public BitmapImage Thumbnail { get; private set; }

        public StorageFolder Folder { get; private set; }

        public string Name
        {
            get
            {
                return Folder.DisplayName;
            }
        }

        public double Percent { get; private set; }

        public string Capacity { get; private set; }

        public ulong TotalByte { get; private set; }

        public ulong FreeByte { get; private set; }

        public string FreeSpace { get; private set; }

        public SolidColorBrush ProgressBarForeground
        {
            get
            {
                if (Percent >= 0.8)
                {
                    return new SolidColorBrush(Colors.Red);
                }
                else
                {
                    return new SolidColorBrush((Color)Application.Current.Resources["SystemAccentColor"]);
                }
            }
        }

        public string StorageSpaceDescription
        {
            get
            {
                if (Globalization.Language == LanguageEnum.Chinese)
                {
                    return FreeSpace + " 可用, 共 " + Capacity;
                }
                else
                {
                    return FreeSpace + " free of " + Capacity;
                }
            }
        }

        public HardDeviceInfo(StorageFolder Device, BitmapImage Thumbnail, IDictionary<string, object> PropertiesRetrieve)
        {
            Folder = Device ?? throw new FileNotFoundException();
            this.Thumbnail = Thumbnail;

            if (PropertiesRetrieve["System.Capacity"] is ulong TotalByte && PropertiesRetrieve["System.FreeSpace"] is ulong FreeByte)
            {
                this.TotalByte = (ulong)PropertiesRetrieve["System.Capacity"];
                this.FreeByte = (ulong)PropertiesRetrieve["System.FreeSpace"];
                Capacity = GetSizeDescription(TotalByte);
                FreeSpace = GetSizeDescription(FreeByte);
                Percent = 1 - FreeByte / Convert.ToDouble(TotalByte);
            }
            else
            {
                Capacity = "Unknown";
                FreeSpace = "Unknown";
                Percent = 0;
            }
        }

        private string GetSizeDescription(ulong Size)
        {
            return Size / 1024f < 1024 ? Math.Round(Size / 1024f, 2).ToString("0.00") + " KB" :
            (Size / 1048576f < 1024 ? Math.Round(Size / 1048576f, 2).ToString("0.00") + " MB" :
            (Size / 1073741824f < 1024 ? Math.Round(Size / 1073741824f, 2).ToString("0.00") + " GB" :
            Math.Round(Size / Convert.ToDouble(1099511627776), 2).ToString("0.00") + " TB"));
        }
    }

    public enum LibrarySource
    {
        SystemBase = 0,
        UserAdded = 1
    }

    public sealed class LibraryFolder
    {
        public string Name { get; private set; }

        public BitmapImage Thumbnail { get; private set; }

        public StorageFolder Folder { get; private set; }

        public string DisplayType
        {
            get
            {
                return Folder.DisplayType;
            }
        }

        public LibrarySource Source { get; private set; }

        public LibraryFolder(StorageFolder Folder, BitmapImage Thumbnail, LibrarySource Source)
        {
            if (Folder == null)
            {
                throw new FileNotFoundException();
            }

            Name = Folder.Name;
            this.Thumbnail = Thumbnail;
            this.Folder = Folder;
            this.Source = Source;
        }
    }
    #endregion

    #region 增量加载集合类
    public sealed class IncrementalLoadingCollection<T> : ObservableCollection<T>, ISupportIncrementalLoading
    {
        private StorageItemQueryResult Query;
        private uint CurrentIndex = 0;
        private Func<uint, uint, StorageItemQueryResult, Task<IEnumerable<T>>> MoreItemsNeed;
        private uint MaxNum = 0;

        public IncrementalLoadingCollection(Func<uint, uint, StorageItemQueryResult, Task<IEnumerable<T>>> MoreItemsNeed)
        {
            this.MoreItemsNeed = MoreItemsNeed;
        }

        public async Task SetStorageItemQueryAsync(StorageItemQueryResult InputQuery)
        {
            Query = InputQuery;

            MaxNum = await Query.GetItemCountAsync();

            CurrentIndex = MaxNum > 100 ? 100 : MaxNum;

            if (MaxNum > 100)
            {
                HasMoreItems = true;
            }
        }

        public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
        {
            return AsyncInfo.Run(async (c) =>
            {
                if (CurrentIndex + count >= MaxNum)
                {
                    uint ItemNeedNum = MaxNum - CurrentIndex;
                    if (ItemNeedNum == 0)
                    {
                        HasMoreItems = false;
                        return new LoadMoreItemsResult { Count = 0 };
                    }
                    else
                    {
                        IEnumerable<T> Result = await MoreItemsNeed(CurrentIndex, ItemNeedNum, Query);

                        for (int i = 0; i < Result.Count() && HasMoreItems; i++)
                        {
                            Add(Result.ElementAt(i));
                        }

                        CurrentIndex = MaxNum;
                        HasMoreItems = false;
                        return new LoadMoreItemsResult { Count = ItemNeedNum };
                    }
                }
                else
                {
                    IEnumerable<T> Result = await MoreItemsNeed(CurrentIndex, count, Query);

                    for (int i = 0; i < Result.Count() && HasMoreItems; i++)
                    {
                        Add(Result.ElementAt(i));
                    }

                    CurrentIndex += count;
                    HasMoreItems = true;
                    return new LoadMoreItemsResult { Count = count };
                }
            });
        }

        public bool HasMoreItems { get; set; } = false;
    }
    #endregion

    #region 网页信息存储类
    /// <summary>
    /// 存储网页信息
    /// </summary>
    public sealed class WebSiteItem
    {
        /// <summary>
        /// 获取网页标题
        /// </summary>
        public string Subject { get; private set; }

        /// <summary>
        /// 获取网址
        /// </summary>
        public string WebSite { get; private set; }

        /// <summary>
        /// 获取网址域名
        /// </summary>
        public string DominName
        {
            get
            {
                return WebSite.StartsWith("https://") ? WebSite.Substring(8) : WebSite.StartsWith("http://") ? WebSite.Substring(7) : WebSite.StartsWith("ftp://") ? WebSite.Substring(6) : null;
            }
        }

        /// <summary>
        /// 指示历史记录树的分类标题前的星形控件的显隐性
        /// </summary>
        public Visibility StarVisibility
        {
            get
            {
                if ((Subject == "今天" || Subject == "昨天" || Subject == "更早") && string.IsNullOrEmpty(WebSite))
                {
                    return Visibility.Collapsed;
                }
                else
                {
                    return Visibility.Visible;
                }
            }
        }

        /// <summary>
        /// 指示字体大小以区分网页内容和分类标题
        /// </summary>
        public double FontSize
        {
            get
            {
                if ((Subject == "今天" || Subject == "昨天" || Subject == "更早") && string.IsNullOrEmpty(WebSite))
                {
                    return 18;
                }
                else
                {
                    return 15;
                }
            }
        }

        /// <summary>
        /// 创建WebSiteItem的新实例
        /// </summary>
        /// <param name="Subject">网页标题</param>
        /// <param name="WebSite">网址</param>
        public WebSiteItem(string Subject, string WebSite)
        {
            this.Subject = Subject;
            this.WebSite = WebSite;
        }
    }
    #endregion

    #region 网页历史记录标志枚举
    /// <summary>
    /// 历史记录分类标题种类枚举
    /// </summary>
    [Flags]
    public enum HistoryTreeCategoryFlag
    {
        Today = 1,
        Yesterday = 2,
        Earlier = 4,
        None = 8
    }
    #endregion

    #region 下载列表模板选择器
    public sealed class DownloadTemplateSelector : DataTemplateSelector
    {
        public DataTemplate DownloadingTemplate { get; set; }
        public DataTemplate DownloadErrorTemplate { get; set; }
        public DataTemplate DownloadCompleteTemplate { get; set; }
        public DataTemplate DownloadCancelTemplate { get; set; }
        public DataTemplate DownloadPauseTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            if (item is DownloadOperator Operator)
            {
                switch (Operator.State)
                {
                    case DownloadState.AlreadyFinished:
                        return DownloadCompleteTemplate;
                    case DownloadState.Canceled:
                        return DownloadCancelTemplate;
                    case DownloadState.Downloading:
                        return DownloadingTemplate;
                    case DownloadState.Error:
                        return DownloadErrorTemplate;
                    case DownloadState.Paused:
                        return DownloadPauseTemplate;
                    default: return null;
                }
            }
            else
            {
                return null;
            }
        }
    }
    #endregion

    #region 快速启动类
    public enum QuickStartType
    {
        Application = 1,
        WebSite = 2,
        UpdateApp = 4,
        UpdateWeb = 8
    }

    public sealed class QuickStartItem : INotifyPropertyChanged
    {
        public BitmapImage Image { get; private set; }

        public string DisplayName { get; private set; }

        public string RelativePath { get; private set; }

        public QuickStartType Type { get; private set; }

        public Uri ProtocalUri { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public void Update(BitmapImage Image, Uri ProtocalUri, string RelativePath, string DisplayName)
        {
            this.Image = Image;
            this.ProtocalUri = ProtocalUri;

            this.DisplayName = DisplayName;

            if (RelativePath != null)
            {
                this.RelativePath = RelativePath;
            }

            OnPropertyChanged("DisplayName");
            OnPropertyChanged("Image");
        }

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public QuickStartItem(BitmapImage Image, Uri Uri, QuickStartType Type, string RelativePath, string DisplayName = null)
        {
            this.Image = Image;
            ProtocalUri = Uri;
            this.Type = Type;

            this.DisplayName = DisplayName;
            this.RelativePath = RelativePath;
        }
    }
    #endregion

    #region 快速启动区域的模板转换器
    public sealed class QuickStartSelector : DataTemplateSelector
    {
        public DataTemplate NormalDataTemplate { get; set; }
        public DataTemplate AddDataTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            if (item is QuickStartItem Start && Start.DisplayName == null)
            {
                return AddDataTemplate;
            }
            else
            {
                return NormalDataTemplate;
            }
        }
    }
    #endregion

    #region WIFI分享功能提供器
    public sealed class WiFiShareProvider : IDisposable
    {
        private HttpListener Listener;

        private bool IsDisposed = false;

        private CancellationTokenSource Cancellation = new CancellationTokenSource();

        public event EventHandler<Exception> ThreadExitedUnexpectly;

        public KeyValuePair<string, string> FilePathMap { get; set; }

        public string CurrentUri { get; private set; }

        public bool IsListeningThreadWorking { get; private set; } = false;

        public WiFiShareProvider()
        {
            Listener = new HttpListener();
            Listener.Prefixes.Add("http://*:8125/");
            Listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;

            HostName CurrentHostName = NetworkInformation.GetHostNames().Where((IP) => IP.Type == HostNameType.Ipv4).FirstOrDefault();
            CurrentUri = "http://" + CurrentHostName + ":8125/";
        }

        public async void StartToListenRequest()
        {
            if (IsListeningThreadWorking)
            {
                return;
            }

            if (IsDisposed)
            {
                throw new ObjectDisposedException("This Object has been disposed");
            }

            IsListeningThreadWorking = true;
            try
            {
                Listener.Start();

                while (true)
                {
                    HttpListenerContext Context = await Listener.GetContextAsync();

                    _ = Task.Factory.StartNew((Para) =>
                     {
                         try
                         {
                             HttpListenerContext HttpContext = Para as HttpListenerContext;
                             if (HttpContext.Request.Url.LocalPath.Substring(1) == FilePathMap.Key)
                             {
                                 StorageFile File = StorageFile.GetFileFromPathAsync(FilePathMap.Value).AsTask().Result;
                                 using (Stream FileStream = File.OpenStreamForReadAsync().Result)
                                 {
                                     Context.Response.ContentLength64 = FileStream.Length;
                                     Context.Response.ContentType = File.ContentType;
                                     Context.Response.AddHeader("Content-Disposition", "Attachment;filename=" + Uri.EscapeDataString(File.Name));

                                     try
                                     {
                                         FileStream.CopyTo(Context.Response.OutputStream);
                                     }
                                     catch (HttpListenerException) { }
                                     finally
                                     {
                                         Context.Response.Close();
                                     }
                                 }
                             }
                             else
                             {
                                 string ErrorMessage = "<html><head><title>Error 404 Bad Request</title></head><body><p style=\"font-size:50px\">HTTP ERROR 404</p><p style=\"font-size:40px\">无法找到指定的资源，请检查URL</p></body></html>";
                                 byte[] SendByte = Encoding.UTF8.GetBytes(ErrorMessage);

                                 Context.Response.StatusCode = 404;
                                 Context.Response.StatusDescription = "Bad Request";
                                 Context.Response.ContentType = "text/html";
                                 Context.Response.ContentLength64 = SendByte.Length;
                                 Context.Response.OutputStream.Write(SendByte, 0, SendByte.Length);
                                 Context.Response.Close();
                             }
                         }
                         catch (Exception e)
                         {
                             ThreadExitedUnexpectly?.Invoke(this, e);
                         }
                     }, Context, Cancellation.Token);
                }
            }
            catch (ObjectDisposedException)
            {
                IsListeningThreadWorking = false;
            }
            catch (Exception e)
            {
                IsListeningThreadWorking = false;
                ThreadExitedUnexpectly?.Invoke(this, e);
            }

            Cancellation?.Dispose();
            Cancellation = null;
        }

        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }
            else
            {
                IsDisposed = true;
                Cancellation?.Cancel();
                Listener.Stop();
                Listener.Abort();
                Listener.Close();
                Listener = null;
            }
        }

        ~WiFiShareProvider()
        {
            Dispose();
        }
    }
    #endregion

    #region 全局背景控制器
    public enum BackgroundBrushType
    {
        Acrylic = 0,
        Picture = 1
    }

    public class BackgroundController : INotifyPropertyChanged
    {
        private readonly AcrylicBrush AcrylicBackgroundBrush;

        private readonly ImageBrush PictureBackgroundBrush;

        private BackgroundBrushType CurrentType;

        public Brush BackgroundBrush
        {
            get
            {
                return CurrentType == BackgroundBrushType.Picture ? PictureBackgroundBrush : (Brush)AcrylicBackgroundBrush;
            }
            set
            {

            }
        }

        private static readonly object Locker = new object();

        private static BackgroundController Instance;

        public event PropertyChangedEventHandler PropertyChanged;

        public static BackgroundController Current
        {
            get
            {
                lock (Locker)
                {
                    return Instance ?? (Instance = new BackgroundController());
                }
            }
        }

        private BackgroundController()
        {
            if (ApplicationData.Current.LocalSettings.Values["UIDisplayMode"] is string Mode)
            {
                if (Mode == "推荐" || Mode == "Recommand")
                {
                    AcrylicBackgroundBrush = new AcrylicBrush
                    {
                        BackgroundSource = AcrylicBackgroundSource.HostBackdrop,
                        TintColor = Colors.LightSlateGray,
                        TintOpacity = 0.4,
                        FallbackColor = Colors.DimGray
                    };
                }
                else
                {
                    AcrylicBackgroundBrush = new AcrylicBrush
                    {
                        BackgroundSource = AcrylicBackgroundSource.HostBackdrop,
                        TintColor = ApplicationData.Current.LocalSettings.Values["AcrylicThemeColor"] is string Color ? GetColorFromHexString(Color) : Colors.LightSlateGray,
                        TintOpacity = 1 - Convert.ToSingle(ApplicationData.Current.LocalSettings.Values["BackgroundTintOpacity"]),
                        TintLuminosityOpacity = 1 - Convert.ToSingle(ApplicationData.Current.LocalSettings.Values["BackgroundTintLuminosity"]),
                        FallbackColor = Colors.DimGray
                    };
                }
            }
            else
            {
                AcrylicBackgroundBrush = new AcrylicBrush
                {
                    BackgroundSource = AcrylicBackgroundSource.HostBackdrop,
                    TintColor = Colors.LightSlateGray,
                    TintOpacity = 0.4,
                    FallbackColor = Colors.DimGray
                };
            }

            if (ApplicationData.Current.LocalSettings.Values["PictureBackgroundUri"] is string uri)
            {
                PictureBackgroundBrush = new ImageBrush
                {
                    ImageSource = new BitmapImage(new Uri(uri)),
                    Stretch = Stretch.UniformToFill
                };
            }
            else
            {
                PictureBackgroundBrush = new ImageBrush
                {
                    Stretch = Stretch.UniformToFill
                };
            }

            if (ApplicationData.Current.LocalSettings.Values["CustomUISubMode"] is string SubMode)
            {
                CurrentType = (BackgroundBrushType)Enum.Parse(typeof(BackgroundBrushType), SubMode);
            }
        }

        public double TintOpacity
        {
            get
            {
                return 1 - (double)AcrylicBackgroundBrush.GetValue(AcrylicBrush.TintOpacityProperty);
            }
            set
            {
                AcrylicBackgroundBrush.SetValue(AcrylicBrush.TintOpacityProperty, 1 - value);
                ApplicationData.Current.LocalSettings.Values["BackgroundTintOpacity"] = value.ToString("0.0");
            }
        }

        public double TintLuminosityOpacity
        {
            get
            {
                return 1 - ((double?)AcrylicBackgroundBrush.GetValue(AcrylicBrush.TintLuminosityOpacityProperty)).GetValueOrDefault();
            }
            set
            {
                if (value == -1)
                {
                    AcrylicBackgroundBrush.SetValue(AcrylicBrush.TintLuminosityOpacityProperty, null);
                }
                else
                {
                    AcrylicBackgroundBrush.SetValue(AcrylicBrush.TintLuminosityOpacityProperty, 1 - value);
                    ApplicationData.Current.LocalSettings.Values["BackgroundTintLuminosity"] = value.ToString("0.0");
                }
            }
        }

        public Color AcrylicColor
        {
            get
            {
                return (Color)AcrylicBackgroundBrush.GetValue(AcrylicBrush.TintColorProperty);
            }
            set
            {
                AcrylicBackgroundBrush.SetValue(AcrylicBrush.TintColorProperty, value);
            }
        }

        public void SwitchTo(BackgroundBrushType Type, string uri = null)
        {
            CurrentType = Type;

            if (Type == BackgroundBrushType.Picture)
            {
                if (string.IsNullOrEmpty(uri))
                {
                    throw new ArgumentNullException("if parameter: 'Type' is BackgroundBrushType.Picture, parameter: 'uri' could not be null or empty");
                }

                if (ApplicationData.Current.LocalSettings.Values["PictureBackgroundUri"] is string Ur)
                {
                    if (Ur != uri)
                    {
                        BitmapImage Bitmap = new BitmapImage();
                        PictureBackgroundBrush.ImageSource = Bitmap;
                        Bitmap.UriSource = new Uri(uri);
                        ApplicationData.Current.LocalSettings.Values["PictureBackgroundUri"] = uri;
                    }
                }
                else
                {
                    BitmapImage Bitmap = new BitmapImage();
                    PictureBackgroundBrush.ImageSource = Bitmap;
                    Bitmap.UriSource = new Uri(uri);
                    ApplicationData.Current.LocalSettings.Values["PictureBackgroundUri"] = uri;
                }
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BackgroundBrush)));
        }

        public Color GetColorFromHexString(string hex)
        {
            hex = hex.Replace("#", string.Empty);

            bool existAlpha = hex.Length == 8 || hex.Length == 4;
            bool isDoubleHex = hex.Length == 8 || hex.Length == 6;

            if (!existAlpha && hex.Length != 6 && hex.Length != 3)
            {
                throw new ArgumentException("输入的hex不是有效颜色");
            }

            int n = 0;
            byte a;
            int hexCount = isDoubleHex ? 2 : 1;
            if (existAlpha)
            {
                n = hexCount;
                a = (byte)ConvertHexToByte(hex, 0, hexCount);
                if (!isDoubleHex)
                {
                    a = (byte)(a * 16 + a);
                }
            }
            else
            {
                a = 0xFF;
            }

            var r = (byte)ConvertHexToByte(hex, n, hexCount);
            var g = (byte)ConvertHexToByte(hex, n + hexCount, hexCount);
            var b = (byte)ConvertHexToByte(hex, n + 2 * hexCount, hexCount);
            if (!isDoubleHex)
            {
                r = (byte)(r * 16 + r);
                g = (byte)(g * 16 + g);
                b = (byte)(b * 16 + b);
            }

            return Color.FromArgb(a, r, g, b);
        }

        private uint ConvertHexToByte(string hex, int n, int count = 2)
        {
            return Convert.ToUInt32(hex.Substring(n, count), 16);
        }
    }
    #endregion

    #region 搜索建议Json解析类
    public class BaiduSearchSuggestionResult
    {
        public string q { get; set; }
        public bool p { get; set; }
        public List<string> s { get; set; }
    }

    public class Suggests
    {
        public string Txt { get; set; }
        public string Type { get; set; }
        public string Sk { get; set; }
        public double HCS { get; set; }
    }

    public class Results
    {
        public string Type { get; set; }
        public List<Suggests> Suggests { get; set; }
    }

    public class AS
    {
        public string Query { get; set; }
        public int FullResults { get; set; }
        public List<Results> Results { get; set; }
    }

    public class BingSearchSuggestionResult
    {
        public AS AS { get; set; }
    }

    #endregion

    #region ContentDialog队列实现
    public class QueueContentDialog : ContentDialog
    {
        private static readonly AutoResetEvent Locker = new AutoResetEvent(true);

        private static int WaitCount = 0;

        public static bool IsRunningOrWaiting
        {
            get
            {
                return WaitCount != 0;
            }
        }

        private bool IsCloseRequested = false;
        private ContentDialogResult CloseWithResult;

        public new async Task<ContentDialogResult> ShowAsync()
        {
            _ = Interlocked.Increment(ref WaitCount);

            await Task.Run(() =>
            {
                Locker.WaitOne();
            });

            var Result = await base.ShowAsync();

            _ = Interlocked.Decrement(ref WaitCount);

            Locker.Set();

            if (IsCloseRequested)
            {
                IsCloseRequested = false;
                return CloseWithResult;
            }
            else
            {
                return Result;
            }
        }

        public void Close(ContentDialogResult CloseWithResult)
        {
            IsCloseRequested = true;
            this.CloseWithResult = CloseWithResult;
            Hide();
        }

        public QueueContentDialog()
        {
            if (AppThemeController.Current.Theme == ElementTheme.Dark)
            {
                Background = Application.Current.Resources["DialogAcrylicBrush"] as Brush;
                RequestedTheme = ElementTheme.Dark;
            }
            else
            {
                RequestedTheme = ElementTheme.Light;
            }
        }
    }
    #endregion

    #region 反馈对象
    public sealed class FeedBackItem : INotifyPropertyChanged
    {
        public string UserName { get; private set; }

        public string Suggestion { get; private set; }

        public string LikeNum { get; private set; }

        public string DislikeNum { get; private set; }

        public string SupportDescription { get; private set; }

        public string Title { get; private set; }

        public string UserID { get; private set; }

        public string GUID { get; private set; }

        public string UserVoteAction { get; set; } = "=";

        public FeedBackItem(string UserName, string Title, string Suggestion, string LikeNum, string DislikeNum, string UserID, string GUID)
        {
            this.UserName = UserName;
            this.Title = Title;
            this.Suggestion = Suggestion;
            this.LikeNum = LikeNum;
            this.DislikeNum = DislikeNum;
            this.UserID = UserID;
            this.GUID = GUID;
            if (Globalization.Language == LanguageEnum.Chinese)
            {
                SupportDescription = $"({LikeNum} 人支持 , {DislikeNum} 人反对)";
            }
            else
            {
                SupportDescription = $"({LikeNum} people agree , {DislikeNum} people against)";
            }
        }

        public void UpdateSupportInfo(FeedBackUpdateType Type, bool IsAdding)
        {
            if (Type == FeedBackUpdateType.Like)
            {
                if (IsAdding)
                {
                    LikeNum = (Convert.ToInt16(LikeNum) + 1).ToString();
                    UserVoteAction = "+";
                }
                else
                {
                    LikeNum = (Convert.ToInt16(LikeNum) - 1).ToString();
                    UserVoteAction = "=";
                }

                SupportDescription = Globalization.Language == LanguageEnum.Chinese
                    ? $"({LikeNum} 人支持 , {DislikeNum} 人反对)"
                    : $"({LikeNum} people agree , {DislikeNum} people against)";
            }
            else
            {
                if (IsAdding)
                {
                    DislikeNum = (Convert.ToInt16(DislikeNum) + 1).ToString();
                    UserVoteAction = "-";
                }
                else
                {
                    DislikeNum = (Convert.ToInt16(DislikeNum) - 1).ToString();
                    UserVoteAction = "=";
                }

                SupportDescription = Globalization.Language == LanguageEnum.Chinese
                    ? $"({LikeNum} 人支持 , {DislikeNum} 人反对)"
                    : $"({LikeNum} people agree , {DislikeNum} people against)";
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SupportDescription)));
        }

        public void UpdateTitleAndSuggestion(string Title, string Suggestion)
        {
            this.Title = Title;
            this.Suggestion = Suggestion;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Title)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Suggestion)));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public enum FeedBackUpdateType
    {
        Like = 0,
        Dislike = 1
    }
    #endregion

    #region 背景图片
    public sealed class BackgroundPicture
    {
        public BitmapImage Picture { get; private set; }

        public Uri PictureUri { get; private set; }

        public BackgroundPicture(BitmapImage Picture, Uri PictureUri)
        {
            this.Picture = Picture;
            this.PictureUri = PictureUri;
        }
    }
    #endregion

    #region 错误类型
    public sealed class PasswordErrorException : Exception
    {
        public PasswordErrorException(string ErrorMessage) : base(ErrorMessage)
        {
        }

        public PasswordErrorException() : base()
        {
        }
    }

    public sealed class FileDamagedException : Exception
    {
        public FileDamagedException(string ErrorMessage) : base(ErrorMessage)
        {
        }

        public FileDamagedException() : base()
        {
        }
    }
    #endregion

    #region 本地化指示器
    public enum LanguageEnum
    {
        Chinese = 1,
        English = 2
    }

    public static class Globalization
    {
        public static LanguageEnum Language { get; private set; }

        static Globalization()
        {
            Language = GlobalizationPreferences.Languages.FirstOrDefault().StartsWith("zh", StringComparison.OrdinalIgnoreCase) ? LanguageEnum.Chinese : LanguageEnum.English;
        }
    }
    #endregion

    #region WindowsHello授权管理器
    public enum AuthenticatorState
    {
        RegisterSuccess = 0,
        UserCanceled = 1,
        CredentialNotFound = 2,
        UnknownError = 4,
        WindowsHelloUnsupport = 8,
        VerifyPassed = 16,
        VerifyFailed = 32,
        UserNotRegistered = 64
    }

    public static class WindowsHelloAuthenticator
    {
        private const string ChallengeText = "This is a challenge send by RX, to verify secure area access authorization";

        private const string CredentialName = "RX-SecureProtection";

        public static Task<bool> CheckSupportAsync()
        {
            return KeyCredentialManager.IsSupportedAsync().AsTask();
        }

        public static async Task<AuthenticatorState> RegisterUserAsync()
        {
            if (await CheckSupportAsync())
            {
                KeyCredentialRetrievalResult CredentiaResult = await KeyCredentialManager.RequestCreateAsync(CredentialName, KeyCredentialCreationOption.ReplaceExisting);
                switch (CredentiaResult.Status)
                {
                    case KeyCredentialStatus.Success:
                        {
                            string PublicKey = CryptographicBuffer.EncodeToHexString(CredentiaResult.Credential.RetrievePublicKey());
                            ApplicationData.Current.LocalSettings.Values["WindowsHelloPublicKeyForUser"] = PublicKey;
                            return AuthenticatorState.RegisterSuccess;
                        }
                    case KeyCredentialStatus.UserCanceled:
                        {
                            return AuthenticatorState.UserCanceled;
                        }
                    default:
                        {
                            return AuthenticatorState.UnknownError;
                        }
                }
            }
            else
            {
                return AuthenticatorState.WindowsHelloUnsupport;
            }
        }

        public static async Task<AuthenticatorState> VerifyUserAsync()
        {
            if (await CheckSupportAsync())
            {
                if (ApplicationData.Current.LocalSettings.Values["WindowsHelloPublicKeyForUser"] is string PublicKey)
                {
                    KeyCredentialRetrievalResult RetrievalResult = await KeyCredentialManager.OpenAsync(CredentialName);
                    switch (RetrievalResult.Status)
                    {
                        case KeyCredentialStatus.Success:
                            {
                                KeyCredentialOperationResult OperationResult = await RetrievalResult.Credential.RequestSignAsync(CryptographicBuffer.ConvertStringToBinary(ChallengeText, BinaryStringEncoding.Utf8));
                                if (OperationResult.Status == KeyCredentialStatus.Success)
                                {
                                    var Algorithm = AsymmetricKeyAlgorithmProvider.OpenAlgorithm(AsymmetricAlgorithmNames.RsaSignPkcs1Sha256);
                                    var Key = Algorithm.ImportPublicKey(CryptographicBuffer.DecodeFromHexString(PublicKey));
                                    return CryptographicEngine.VerifySignature(Key, CryptographicBuffer.ConvertStringToBinary(ChallengeText, BinaryStringEncoding.Utf8), OperationResult.Result) ? AuthenticatorState.VerifyPassed : AuthenticatorState.VerifyFailed;
                                }
                                else
                                {
                                    return AuthenticatorState.UnknownError;
                                }
                            }
                        case KeyCredentialStatus.NotFound:
                            {
                                return AuthenticatorState.CredentialNotFound;
                            }
                        default:
                            {
                                return AuthenticatorState.UnknownError;
                            }
                    }
                }
                else
                {
                    return AuthenticatorState.UserNotRegistered;
                }
            }
            else
            {
                return AuthenticatorState.WindowsHelloUnsupport;
            }
        }

        public static async Task DeleteUserAsync()
        {
            if (await CheckSupportAsync())
            {
                try
                {
                    ApplicationData.Current.LocalSettings.Values["WindowsHelloPublicKeyForUser"] = null;
                    await KeyCredentialManager.DeleteAsync(CredentialName);
                }
                catch (Exception)
                {
                }
            }
        }
    }
    #endregion

    #region 密码生成器
    public static class KeyGenerator
    {
        public static string GetMD5FromKey(string OriginKey, int Length = 32)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(OriginKey);
                byte[] Md5Buffer = md5.ComputeHash(bytes);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < Md5Buffer.Length; i++)
                {
                    _ = sb.Append(Md5Buffer[i].ToString("x2"));
                }

                if (Length <= 32)
                {
                    return sb.ToString().Substring((32 - Length) / 2, Length);
                }
                else
                {
                    string Result = sb.ToString();
                    return Result + Result.Substring(0, Length - 32);
                }
            }
        }

        public static string GetRandomKey(uint Length)
        {
            StringBuilder Builder = new StringBuilder();
            Random CharNumRandom = new Random();

            for (int i = 0; i < Length; i++)
            {
                switch (CharNumRandom.Next(0, 3))
                {
                    case 0:
                        {
                            _ = Builder.Append((char)CharNumRandom.Next(65, 91));
                            break;
                        }
                    case 1:
                        {
                            _ = Builder.Append((char)CharNumRandom.Next(97, 123));
                            break;
                        }
                    case 2:
                        {
                            _ = Builder.Append((char)CharNumRandom.Next(48, 58));
                            break;
                        }
                }
            }

            return Builder.ToString();
        }
    }
    #endregion

    #region 凭据保护器
    public static class CredentialProtector
    {
        public static string GetPasswordFromProtector(string Name)
        {
            PasswordVault Vault = new PasswordVault();

            PasswordCredential Credential = Vault.RetrieveAll().Where((Cre) => Cre.UserName == Name).FirstOrDefault();

            if (Credential != null)
            {
                Credential.RetrievePassword();
                return Credential.Password;
            }
            else
            {
                return string.Empty;
            }
        }

        public static void RequestProtectPassword(string Name, string Password)
        {
            PasswordVault Vault = new PasswordVault();

            PasswordCredential Credential = Vault.RetrieveAll().Where((Cre) => Cre.UserName == Name).FirstOrDefault();

            if (Credential != null)
            {
                Vault.Remove(Credential);
            }

            Vault.Add(new PasswordCredential("RX_Secure_Vault", Name, Password));
        }
    }
    #endregion

    #region 通用文件转换器
    public static class GeneralTransformer
    {
        private static CancellationTokenSource AVTranscodeCancellation;

        public static bool IsAnyTransformTaskRunning { get; private set; } = false;

        public static Task GenerateMergeVideoFromOriginAsync(StorageFile DestinationFile, MediaComposition Composition, MediaEncodingProfile EncodingProfile)
        {
            return Task.Factory.StartNew((ob) =>
            {
                IsAnyTransformTaskRunning = true;

                AVTranscodeCancellation = new CancellationTokenSource();

                var Para = (ValueTuple<StorageFile, MediaComposition, MediaEncodingProfile>)ob;

                SendUpdatableToastWithProgressForMergeVideo();
                Progress<double> CropVideoProgress = new Progress<double>((CurrentValue) =>
                {
                    string Tag = "MergeVideoNotification";

                    var data = new NotificationData
                    {
                        SequenceNumber = 0
                    };
                    data.Values["ProgressValue"] = Math.Round(CurrentValue / 100, 2, MidpointRounding.AwayFromZero).ToString();
                    data.Values["ProgressValueString"] = Convert.ToInt32(CurrentValue) + "%";

                    ToastNotificationManager.CreateToastNotifier().Update(data, Tag);
                });

                try
                {
                    Para.Item2.RenderToFileAsync(Para.Item1, MediaTrimmingPreference.Precise, Para.Item3).AsTask(AVTranscodeCancellation.Token, CropVideoProgress).Wait();
                    ApplicationData.Current.LocalSettings.Values["MediaMergeStatus"] = "Success";
                }
                catch (AggregateException)
                {
                    ApplicationData.Current.LocalSettings.Values["MediaMergeStatus"] = "Cancel";
                    Para.Item1.DeleteAsync(StorageDeleteOption.PermanentDelete).AsTask().Wait();
                }
                catch (Exception e)
                {
                    ApplicationData.Current.LocalSettings.Values["MediaMergeStatus"] = e.Message;
                    Para.Item1.DeleteAsync(StorageDeleteOption.PermanentDelete).AsTask().Wait();
                }

            }, (DestinationFile, Composition, EncodingProfile), TaskCreationOptions.LongRunning).ContinueWith((task) =>
            {
                CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    if (Globalization.Language == LanguageEnum.Chinese)
                    {
                        switch (ApplicationData.Current.LocalSettings.Values["MediaMergeStatus"].ToString())
                        {
                            case "Success":
                                {
                                    FileControl.ThisPage.Notification.Show("视频已成功完成合并", 5000);
                                    ShowMergeCompleteNotification();
                                    break;
                                }
                            case "Cancel":
                                {
                                    FileControl.ThisPage.Notification.Show("视频合并任务被取消", 5000);
                                    ShowMergeCancelNotification();
                                    break;
                                }
                            default:
                                {
                                    FileControl.ThisPage.Notification.Show("合并视频时遇到未知错误", 5000);
                                    break;
                                }
                        }
                    }
                    else
                    {
                        switch (ApplicationData.Current.LocalSettings.Values["MediaMergeStatus"].ToString())
                        {
                            case "Success":
                                {
                                    FileControl.ThisPage.Notification.Show("Video successfully merged", 5000);
                                    ShowMergeCompleteNotification();
                                    break;
                                }
                            case "Cancel":
                                {
                                    FileControl.ThisPage.Notification.Show("Video merge task was canceled", 5000);
                                    ShowMergeCancelNotification();
                                    break;
                                }
                            default:
                                {
                                    FileControl.ThisPage.Notification.Show("Encountered unknown error while merging video", 5000);
                                    break;
                                }
                        }
                    }
                }).AsTask().Wait();

                IsAnyTransformTaskRunning = false;
            });
        }

        public static Task GenerateCroppedVideoFromOriginAsync(StorageFile DestinationFile, MediaComposition Composition, MediaEncodingProfile EncodingProfile, MediaTrimmingPreference TrimmingPreference)
        {
            return Task.Factory.StartNew((ob) =>
            {
                IsAnyTransformTaskRunning = true;

                AVTranscodeCancellation = new CancellationTokenSource();

                var Para = (ValueTuple<StorageFile, MediaComposition, MediaEncodingProfile, MediaTrimmingPreference>)ob;

                SendUpdatableToastWithProgressForCropVideo(Para.Item1);
                Progress<double> CropVideoProgress = new Progress<double>((CurrentValue) =>
                {
                    string Tag = "CropVideoNotification";

                    var data = new NotificationData
                    {
                        SequenceNumber = 0
                    };
                    data.Values["ProgressValue"] = Math.Round(CurrentValue / 100, 2, MidpointRounding.AwayFromZero).ToString();
                    data.Values["ProgressValueString"] = Convert.ToInt32(CurrentValue) + "%";

                    ToastNotificationManager.CreateToastNotifier().Update(data, Tag);
                });

                try
                {
                    Para.Item2.RenderToFileAsync(Para.Item1, Para.Item4, Para.Item3).AsTask(AVTranscodeCancellation.Token, CropVideoProgress).Wait();
                    ApplicationData.Current.LocalSettings.Values["MediaCropStatus"] = "Success";
                }
                catch (AggregateException)
                {
                    ApplicationData.Current.LocalSettings.Values["MediaCropStatus"] = "Cancel";
                    Para.Item1.DeleteAsync(StorageDeleteOption.PermanentDelete).AsTask().Wait();
                }
                catch (Exception e)
                {
                    ApplicationData.Current.LocalSettings.Values["MediaCropStatus"] = e.Message;
                    Para.Item1.DeleteAsync(StorageDeleteOption.PermanentDelete).AsTask().Wait();
                }

            }, (DestinationFile, Composition, EncodingProfile, TrimmingPreference), TaskCreationOptions.LongRunning).ContinueWith((task) =>
             {
                 CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                 {
                     if (Globalization.Language == LanguageEnum.Chinese)
                     {
                         switch (ApplicationData.Current.LocalSettings.Values["MediaCropStatus"].ToString())
                         {
                             case "Success":
                                 {
                                     FileControl.ThisPage.Notification.Show("视频已成功完成裁剪", 5000);
                                     ShowCropCompleteNotification();
                                     break;
                                 }
                             case "Cancel":
                                 {
                                     FileControl.ThisPage.Notification.Show("视频裁剪任务被取消", 5000);
                                     ShowCropCancelNotification();
                                     break;
                                 }
                             default:
                                 {
                                     FileControl.ThisPage.Notification.Show("裁剪视频时遇到未知错误", 5000);
                                     break;
                                 }
                         }
                     }
                     else
                     {
                         switch (ApplicationData.Current.LocalSettings.Values["MediaCropStatus"].ToString())
                         {
                             case "Success":
                                 {
                                     FileControl.ThisPage.Notification.Show("Video successfully cropped", 5000);
                                     ShowCropCompleteNotification();
                                     break;
                                 }
                             case "Cancel":
                                 {
                                     FileControl.ThisPage.Notification.Show("Video crop task was canceled", 5000);
                                     ShowCropCancelNotification();
                                     break;
                                 }
                             default:
                                 {
                                     FileControl.ThisPage.Notification.Show("Encountered unknown error while cropping video", 5000);
                                     break;
                                 }
                         }
                     }
                 }).AsTask().Wait();

                 IsAnyTransformTaskRunning = false;
             });
        }

        public static Task TranscodeFromImageAsync(StorageFile SourceFile, StorageFile DestinationFile, bool IsEnableScale = false, uint ScaleWidth = default, uint ScaleHeight = default, BitmapInterpolationMode InterpolationMode = default)
        {
            return Task.Run(() =>
            {
                IsAnyTransformTaskRunning = true;

                using (var OriginStream = SourceFile.OpenAsync(FileAccessMode.Read).AsTask().Result)
                {
                    BitmapDecoder Decoder = BitmapDecoder.CreateAsync(OriginStream).AsTask().Result;
                    using (var TargetStream = DestinationFile.OpenAsync(FileAccessMode.ReadWrite).AsTask().Result)
                    using (var TranscodeImage = Decoder.GetSoftwareBitmapAsync().AsTask().Result)
                    {
                        BitmapEncoder Encoder = DestinationFile.FileType switch
                        {
                            ".png" => BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, TargetStream).AsTask().Result,
                            ".jpg" => BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, TargetStream).AsTask().Result,
                            ".bmp" => BitmapEncoder.CreateAsync(BitmapEncoder.BmpEncoderId, TargetStream).AsTask().Result,
                            ".heic" => BitmapEncoder.CreateAsync(BitmapEncoder.HeifEncoderId, TargetStream).AsTask().Result,
                            ".tiff" => BitmapEncoder.CreateAsync(BitmapEncoder.TiffEncoderId, TargetStream).AsTask().Result,
                            _ => throw new InvalidOperationException("Unsupport image format"),
                        };

                        if (IsEnableScale)
                        {
                            Encoder.BitmapTransform.ScaledWidth = ScaleWidth;
                            Encoder.BitmapTransform.ScaledHeight = ScaleHeight;
                            Encoder.BitmapTransform.InterpolationMode = InterpolationMode;
                        }

                        Encoder.SetSoftwareBitmap(TranscodeImage);
                        Encoder.IsThumbnailGenerated = true;
                        try
                        {
                            Encoder.FlushAsync().AsTask().Wait();
                        }
                        catch (Exception err)
                        {
                            if (err.HResult == unchecked((int)0x88982F81))
                            {
                                Encoder.IsThumbnailGenerated = false;
                                Encoder.FlushAsync().AsTask().Wait();
                            }
                            else
                            {
                                throw;
                            }
                        }
                    }
                }

                IsAnyTransformTaskRunning = false;
            });
        }

        public static Task TranscodeFromAudioOrVideoAsync(StorageFile SourceFile, StorageFile DestinationFile, string MediaTranscodeEncodingProfile, string MediaTranscodeQuality, bool SpeedUp)
        {
            return Task.Factory.StartNew((ob) =>
            {
                IsAnyTransformTaskRunning = true;

                AVTranscodeCancellation = new CancellationTokenSource();

                var Para = (ValueTuple<StorageFile, StorageFile, string, string, bool>)ob;

                MediaTranscoder Transcoder = new MediaTranscoder
                {
                    HardwareAccelerationEnabled = true,
                    VideoProcessingAlgorithm = Para.Item5 ? MediaVideoProcessingAlgorithm.Default : MediaVideoProcessingAlgorithm.MrfCrf444
                };

                try
                {
                    MediaEncodingProfile Profile = null;
                    VideoEncodingQuality VideoQuality = default;
                    AudioEncodingQuality AudioQuality = default;

                    switch (Para.Item4)
                    {
                        case "UHD2160p":
                            VideoQuality = VideoEncodingQuality.Uhd2160p;
                            break;
                        case "QVGA":
                            VideoQuality = VideoEncodingQuality.Qvga;
                            break;
                        case "HD1080p":
                            VideoQuality = VideoEncodingQuality.HD1080p;
                            break;
                        case "HD720p":
                            VideoQuality = VideoEncodingQuality.HD720p;
                            break;
                        case "WVGA":
                            VideoQuality = VideoEncodingQuality.Wvga;
                            break;
                        case "VGA":
                            VideoQuality = VideoEncodingQuality.Vga;
                            break;
                        case "High":
                            AudioQuality = AudioEncodingQuality.High;
                            break;
                        case "Medium":
                            AudioQuality = AudioEncodingQuality.Medium;
                            break;
                        case "Low":
                            AudioQuality = AudioEncodingQuality.Low;
                            break;
                    }

                    switch (Para.Item3)
                    {
                        case "MKV":
                            Profile = MediaEncodingProfile.CreateHevc(VideoQuality);
                            break;
                        case "MP4":
                            Profile = MediaEncodingProfile.CreateMp4(VideoQuality);
                            break;
                        case "WMV":
                            Profile = MediaEncodingProfile.CreateWmv(VideoQuality);
                            break;
                        case "AVI":
                            Profile = MediaEncodingProfile.CreateAvi(VideoQuality);
                            break;
                        case "MP3":
                            Profile = MediaEncodingProfile.CreateMp3(AudioQuality);
                            break;
                        case "ALAC":
                            Profile = MediaEncodingProfile.CreateAlac(AudioQuality);
                            break;
                        case "WMA":
                            Profile = MediaEncodingProfile.CreateWma(AudioQuality);
                            break;
                        case "M4A":
                            Profile = MediaEncodingProfile.CreateM4a(AudioQuality);
                            break;
                    }

                    PrepareTranscodeResult Result = Transcoder.PrepareFileTranscodeAsync(Para.Item1, Para.Item2, Profile).AsTask().Result;
                    if (Result.CanTranscode)
                    {
                        SendUpdatableToastWithProgressForTranscode(Para.Item1, Para.Item2);
                        Progress<double> TranscodeProgress = new Progress<double>((CurrentValue) =>
                        {
                            string Tag = "TranscodeNotification";

                            var data = new NotificationData
                            {
                                SequenceNumber = 0
                            };
                            data.Values["ProgressValue"] = (Math.Ceiling(CurrentValue) / 100).ToString();
                            data.Values["ProgressValueString"] = Convert.ToInt32(CurrentValue) + "%";

                            ToastNotificationManager.CreateToastNotifier().Update(data, Tag);
                        });

                        Result.TranscodeAsync().AsTask(AVTranscodeCancellation.Token, TranscodeProgress).Wait();

                        ApplicationData.Current.LocalSettings.Values["MediaTranscodeStatus"] = "Success";
                    }
                    else
                    {
                        ApplicationData.Current.LocalSettings.Values["MediaTranscodeStatus"] = "NotSupport";
                        Para.Item2.DeleteAsync(StorageDeleteOption.PermanentDelete).AsTask().Wait();
                    }
                }
                catch (AggregateException)
                {
                    ApplicationData.Current.LocalSettings.Values["MediaTranscodeStatus"] = "Cancel";
                    Para.Item2.DeleteAsync(StorageDeleteOption.PermanentDelete).AsTask().Wait();
                }
                catch (Exception e)
                {
                    ApplicationData.Current.LocalSettings.Values["MediaTranscodeStatus"] = e.Message;
                    Para.Item2.DeleteAsync(StorageDeleteOption.PermanentDelete).AsTask().Wait();
                }
            }, (SourceFile, DestinationFile, MediaTranscodeEncodingProfile, MediaTranscodeQuality, SpeedUp), TaskCreationOptions.LongRunning).ContinueWith((task, ob) =>
             {
                 AVTranscodeCancellation.Dispose();
                 AVTranscodeCancellation = null;

                 var Para = (ValueTuple<StorageFile, StorageFile>)ob;

                 if (ApplicationData.Current.LocalSettings.Values["MediaTranscodeStatus"] is string ExcuteStatus)
                 {
                     CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                     {
                         if (Globalization.Language == LanguageEnum.Chinese)
                         {
                             switch (ExcuteStatus)
                             {
                                 case "Success":
                                     FileControl.ThisPage.Notification.Show("转码已成功完成", 5000);
                                     ShowTranscodeCompleteNotification(Para.Item1, Para.Item2);
                                     break;
                                 case "Cancel":
                                     FileControl.ThisPage.Notification.Show("转码任务被取消", 5000);
                                     ShowTranscodeCancelNotification();
                                     break;
                                 case "NotSupport":
                                     FileControl.ThisPage.Notification.Show("转码格式不支持", 5000);
                                     break;
                                 default:
                                     FileControl.ThisPage.Notification.Show("转码失败:" + ExcuteStatus, 5000);
                                     break;
                             }
                         }
                         else
                         {
                             switch (ExcuteStatus)
                             {
                                 case "Success":
                                     FileControl.ThisPage.Notification.Show("Transcoding has been successfully completed", 5000);
                                     ShowTranscodeCompleteNotification(Para.Item1, Para.Item2);
                                     break;
                                 case "Cancel":
                                     FileControl.ThisPage.Notification.Show("Transcoding task is cancelled", 5000);
                                     ShowTranscodeCancelNotification();
                                     break;
                                 case "NotSupport":
                                     FileControl.ThisPage.Notification.Show("Transcoding format is not supported", 5000);
                                     break;
                                 default:
                                     FileControl.ThisPage.Notification.Show("Transcoding failed:" + ExcuteStatus, 5000);
                                     break;
                             }
                         }
                     }).AsTask().Wait();
                 }

                 IsAnyTransformTaskRunning = false;

             }, (SourceFile, DestinationFile));
        }

        private static void SendUpdatableToastWithProgressForCropVideo(StorageFile SourceFile)
        {
            var content = new ToastContent()
            {
                Launch = "Transcode",
                Scenario = ToastScenario.Reminder,
                Visual = new ToastVisual()
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
                        {
                            new AdaptiveText()
                            {
                                Text = Globalization.Language==LanguageEnum.Chinese
                                ? ("正在裁剪:"+SourceFile.DisplayName)
                                : ("Cropping:"+SourceFile.DisplayName)
                            },

                            new AdaptiveProgressBar()
                            {
                                Title = Globalization.Language==LanguageEnum.Chinese?"正在处理...":"Processing",
                                Value = new BindableProgressBarValue("ProgressValue"),
                                ValueStringOverride = new BindableString("ProgressValueString"),
                                Status = new BindableString("ProgressStatus")
                            }
                        }
                    }
                }
            };

            var Toast = new ToastNotification(content.GetXml())
            {
                Tag = "CropVideoNotification",
                Data = new NotificationData()
            };
            Toast.Data.Values["ProgressValue"] = "0";
            Toast.Data.Values["ProgressValueString"] = "0%";
            Toast.Data.Values["ProgressStatus"] = Globalization.Language == LanguageEnum.Chinese
                ? "点击该提示以取消"
                : "Click the prompt to cancel";
            Toast.Data.SequenceNumber = 0;

            Toast.Activated += (s, e) =>
            {
                if (s.Tag == "CropVideoNotification")
                {
                    AVTranscodeCancellation.Cancel();
                }
            };

            ToastNotificationManager.CreateToastNotifier().Show(Toast);
        }

        private static void SendUpdatableToastWithProgressForMergeVideo()
        {
            var content = new ToastContent()
            {
                Launch = "Transcode",
                Scenario = ToastScenario.Reminder,
                Visual = new ToastVisual()
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
                        {
                            new AdaptiveText()
                            {
                                Text = Globalization.Language==LanguageEnum.Chinese
                                ? "正在合并视频文件..."
                                : "Merging the video..."
                            },

                            new AdaptiveProgressBar()
                            {
                                Title = Globalization.Language==LanguageEnum.Chinese?"正在处理...":"Processing",
                                Value = new BindableProgressBarValue("ProgressValue"),
                                ValueStringOverride = new BindableString("ProgressValueString"),
                                Status = new BindableString("ProgressStatus")
                            }
                        }
                    }
                }
            };

            var Toast = new ToastNotification(content.GetXml())
            {
                Tag = "MergeVideoNotification",
                Data = new NotificationData()
            };
            Toast.Data.Values["ProgressValue"] = "0";
            Toast.Data.Values["ProgressValueString"] = "0%";
            Toast.Data.Values["ProgressStatus"] = Globalization.Language == LanguageEnum.Chinese
                ? "点击该提示以取消"
                : "Click the prompt to cancel";
            Toast.Data.SequenceNumber = 0;

            Toast.Activated += (s, e) =>
            {
                if (s.Tag == "MergeVideoNotification")
                {
                    AVTranscodeCancellation.Cancel();
                }
            };

            ToastNotificationManager.CreateToastNotifier().Show(Toast);
        }

        private static void SendUpdatableToastWithProgressForTranscode(StorageFile SourceFile, StorageFile DestinationFile)
        {
            string Tag = "TranscodeNotification";

            var content = new ToastContent()
            {
                Launch = "Transcode",
                Scenario = ToastScenario.Reminder,
                Visual = new ToastVisual()
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
                        {
                            new AdaptiveText()
                            {
                                Text = Globalization.Language==LanguageEnum.Chinese
                                ? ("正在转换:"+SourceFile.DisplayName)
                                : ("Transcoding:"+SourceFile.DisplayName)
                            },

                            new AdaptiveProgressBar()
                            {
                                Title = SourceFile.FileType.Substring(1).ToUpper()+" ⋙⋙⋙⋙ "+DestinationFile.FileType.Substring(1).ToUpper(),
                                Value = new BindableProgressBarValue("ProgressValue"),
                                ValueStringOverride = new BindableString("ProgressValueString"),
                                Status = new BindableString("ProgressStatus")
                            }
                        }
                    }
                }
            };

            var Toast = new ToastNotification(content.GetXml())
            {
                Tag = Tag,
                Data = new NotificationData()
            };
            Toast.Data.Values["ProgressValue"] = "0";
            Toast.Data.Values["ProgressValueString"] = "0%";
            Toast.Data.Values["ProgressStatus"] = Globalization.Language == LanguageEnum.Chinese
                ? "点击该提示以取消"
                : "Click the prompt to cancel";
            Toast.Data.SequenceNumber = 0;

            Toast.Activated += (s, e) =>
            {
                if (s.Tag == "TranscodeNotification")
                {
                    AVTranscodeCancellation.Cancel();
                }
            };

            ToastNotificationManager.CreateToastNotifier().Show(Toast);
        }

        private static void ShowCropCompleteNotification()
        {
            ToastNotificationManager.History.Remove("CropVideoNotification");

            if (Globalization.Language == LanguageEnum.Chinese)
            {
                var Content = new ToastContent()
                {
                    Scenario = ToastScenario.Default,
                    Launch = "Transcode",
                    Visual = new ToastVisual()
                    {
                        BindingGeneric = new ToastBindingGeneric()
                        {
                            Children =
                            {
                                new AdaptiveText()
                                {
                                    Text = "裁剪已完成！"
                                },

                                new AdaptiveText()
                                {
                                    Text = "点击以消除提示"
                                }
                            }
                        }
                    },
                };
                ToastNotificationManager.CreateToastNotifier().Show(new ToastNotification(Content.GetXml()));
            }
            else
            {
                var Content = new ToastContent()
                {
                    Scenario = ToastScenario.Default,
                    Launch = "Transcode",
                    Visual = new ToastVisual()
                    {
                        BindingGeneric = new ToastBindingGeneric()
                        {
                            Children =
                            {
                                new AdaptiveText()
                                {
                                    Text = "Cropping has been completed！"
                                },

                                new AdaptiveText()
                                {
                                    Text = "Click to remove the prompt"
                                }
                            }
                        }
                    },
                };
                ToastNotificationManager.CreateToastNotifier().Show(new ToastNotification(Content.GetXml()));
            }
        }

        private static void ShowMergeCompleteNotification()
        {
            ToastNotificationManager.History.Remove("MergeVideoNotification");

            if (Globalization.Language == LanguageEnum.Chinese)
            {
                var Content = new ToastContent()
                {
                    Scenario = ToastScenario.Default,
                    Launch = "Transcode",
                    Visual = new ToastVisual()
                    {
                        BindingGeneric = new ToastBindingGeneric()
                        {
                            Children =
                            {
                                new AdaptiveText()
                                {
                                    Text = "合并已完成！"
                                },

                                new AdaptiveText()
                                {
                                    Text = "点击以消除提示"
                                }
                            }
                        }
                    },
                };
                ToastNotificationManager.CreateToastNotifier().Show(new ToastNotification(Content.GetXml()));
            }
            else
            {
                var Content = new ToastContent()
                {
                    Scenario = ToastScenario.Default,
                    Launch = "Transcode",
                    Visual = new ToastVisual()
                    {
                        BindingGeneric = new ToastBindingGeneric()
                        {
                            Children =
                            {
                                new AdaptiveText()
                                {
                                    Text = "Merging has been completed！"
                                },

                                new AdaptiveText()
                                {
                                    Text = "Click to remove the prompt"
                                }
                            }
                        }
                    },
                };
                ToastNotificationManager.CreateToastNotifier().Show(new ToastNotification(Content.GetXml()));
            }
        }

        private static void ShowTranscodeCompleteNotification(StorageFile SourceFile, StorageFile DestinationFile)
        {
            ToastNotificationManager.History.Remove("TranscodeNotification");

            if (Globalization.Language == LanguageEnum.Chinese)
            {
                var Content = new ToastContent()
                {
                    Scenario = ToastScenario.Default,
                    Launch = "Transcode",
                    Visual = new ToastVisual()
                    {
                        BindingGeneric = new ToastBindingGeneric()
                        {
                            Children =
                            {
                                new AdaptiveText()
                                {
                                    Text = "转换已完成！"
                                },

                                new AdaptiveText()
                                {
                                   Text = SourceFile.Name + " 已成功转换为 " + DestinationFile.Name
                                },

                                new AdaptiveText()
                                {
                                    Text = "点击以消除提示"
                                }
                            }
                        }
                    },
                };
                ToastNotificationManager.CreateToastNotifier().Show(new ToastNotification(Content.GetXml()));
            }
            else
            {
                var Content = new ToastContent()
                {
                    Scenario = ToastScenario.Default,
                    Launch = "Transcode",
                    Visual = new ToastVisual()
                    {
                        BindingGeneric = new ToastBindingGeneric()
                        {
                            Children =
                            {
                                new AdaptiveText()
                                {
                                    Text = "Transcoding has been completed！"
                                },

                                new AdaptiveText()
                                {
                                   Text = SourceFile.Name + " has been successfully transcoded to " + DestinationFile.Name
                                },

                                new AdaptiveText()
                                {
                                    Text = "Click to remove the prompt"
                                }
                            }
                        }
                    },
                };
                ToastNotificationManager.CreateToastNotifier().Show(new ToastNotification(Content.GetXml()));
            }
        }

        private static void ShowCropCancelNotification()
        {
            ToastNotificationManager.History.Remove("CropVideoNotification");

            if (Globalization.Language == LanguageEnum.Chinese)
            {
                var Content = new ToastContent()
                {
                    Scenario = ToastScenario.Default,
                    Launch = "Transcode",
                    Visual = new ToastVisual()
                    {
                        BindingGeneric = new ToastBindingGeneric()
                        {
                            Children =
                            {
                                new AdaptiveText()
                                {
                                    Text = "裁剪任务已被取消"
                                },

                                new AdaptiveText()
                                {
                                   Text = "您可以尝试重新启动此任务"
                                },

                                new AdaptiveText()
                                {
                                    Text = "点击以消除提示"
                                }
                            }
                        }
                    }
                };
                ToastNotificationManager.CreateToastNotifier().Show(new ToastNotification(Content.GetXml()));
            }
            else
            {
                var Content = new ToastContent()
                {
                    Scenario = ToastScenario.Default,
                    Launch = "Transcode",
                    Visual = new ToastVisual()
                    {
                        BindingGeneric = new ToastBindingGeneric()
                        {
                            Children =
                            {
                                new AdaptiveText()
                                {
                                    Text = "Cropping task has been cancelled"
                                },

                                new AdaptiveText()
                                {
                                   Text = "You can try restarting the task"
                                },

                                new AdaptiveText()
                                {
                                    Text = "Click to remove the prompt"
                                }
                            }
                        }
                    }
                };
                ToastNotificationManager.CreateToastNotifier().Show(new ToastNotification(Content.GetXml()));
            }
        }

        private static void ShowMergeCancelNotification()
        {
            ToastNotificationManager.History.Remove("MergeVideoNotification");

            if (Globalization.Language == LanguageEnum.Chinese)
            {
                var Content = new ToastContent()
                {
                    Scenario = ToastScenario.Default,
                    Launch = "Transcode",
                    Visual = new ToastVisual()
                    {
                        BindingGeneric = new ToastBindingGeneric()
                        {
                            Children =
                            {
                                new AdaptiveText()
                                {
                                    Text = "合并任务已被取消"
                                },

                                new AdaptiveText()
                                {
                                   Text = "您可以尝试重新启动此任务"
                                },

                                new AdaptiveText()
                                {
                                    Text = "点击以消除提示"
                                }
                            }
                        }
                    }
                };
                ToastNotificationManager.CreateToastNotifier().Show(new ToastNotification(Content.GetXml()));
            }
            else
            {
                var Content = new ToastContent()
                {
                    Scenario = ToastScenario.Default,
                    Launch = "Transcode",
                    Visual = new ToastVisual()
                    {
                        BindingGeneric = new ToastBindingGeneric()
                        {
                            Children =
                            {
                                new AdaptiveText()
                                {
                                    Text = "Merging task has been cancelled"
                                },

                                new AdaptiveText()
                                {
                                   Text = "You can try restarting the task"
                                },

                                new AdaptiveText()
                                {
                                    Text = "Click to remove the prompt"
                                }
                            }
                        }
                    }
                };
                ToastNotificationManager.CreateToastNotifier().Show(new ToastNotification(Content.GetXml()));
            }
        }

        private static void ShowTranscodeCancelNotification()
        {
            ToastNotificationManager.History.Remove("TranscodeNotification");

            if (Globalization.Language == LanguageEnum.Chinese)
            {
                var Content = new ToastContent()
                {
                    Scenario = ToastScenario.Default,
                    Launch = "Transcode",
                    Visual = new ToastVisual()
                    {
                        BindingGeneric = new ToastBindingGeneric()
                        {
                            Children =
                            {
                                new AdaptiveText()
                                {
                                    Text = "格式转换已被取消"
                                },

                                new AdaptiveText()
                                {
                                   Text = "您可以尝试重新启动转换"
                                },

                                new AdaptiveText()
                                {
                                    Text = "点击以消除提示"
                                }
                            }
                        }
                    }
                };
                ToastNotificationManager.CreateToastNotifier().Show(new ToastNotification(Content.GetXml()));
            }
            else
            {
                var Content = new ToastContent()
                {
                    Scenario = ToastScenario.Default,
                    Launch = "Transcode",
                    Visual = new ToastVisual()
                    {
                        BindingGeneric = new ToastBindingGeneric()
                        {
                            Children =
                            {
                                new AdaptiveText()
                                {
                                    Text = "Transcode has been cancelled"
                                },

                                new AdaptiveText()
                                {
                                   Text = "You can try restarting the transcode"
                                },

                                new AdaptiveText()
                                {
                                    Text = "Click to remove the prompt"
                                }
                            }
                        }
                    }
                };
                ToastNotificationManager.CreateToastNotifier().Show(new ToastNotification(Content.GetXml()));
            }
        }
    }

    #endregion

    #region 时间和反转布尔值转换器
    public sealed class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool v)
            {
                if (targetType == typeof(Visibility))
                {
                    return v ? Visibility.Collapsed : Visibility.Visible;
                }
                else if (targetType == typeof(bool))
                {
                    return !v;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is bool v)
            {
                if (targetType == typeof(Visibility))
                {
                    return v ? Visibility.Collapsed : Visibility.Visible;
                }
                else if (targetType == typeof(bool))
                {
                    return !v;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }
    }

    public sealed class TimespanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is double)
            {
                long Millisecond = System.Convert.ToInt64(value);
                int Hour = 0;
                int Minute = 0;
                int Second = 0;

                if (Millisecond >= 1000)
                {
                    Second = System.Convert.ToInt32(Millisecond / 1000);
                    Millisecond %= 1000;
                    if (Second >= 60)
                    {
                        Minute = Second / 60;
                        Second %= 60;
                        if (Minute >= 60)
                        {
                            Hour = Minute / 60;
                            Minute %= 60;
                        }
                    }
                }
                return string.Format("{0:D2}:{1:D2}:{2:D2}.{3:D3}", Hour, Minute, Second, Millisecond);
            }
            else
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
    #endregion

    #region 主题转换器
    public sealed class AppThemeController : INotifyPropertyChanged
    {
        public ElementTheme Theme { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private static AppThemeController Instance;

        private static readonly object Lock = new object();

        public static AppThemeController Current
        {
            get
            {
                lock (Lock)
                {
                    return Instance ?? (Instance = new AppThemeController());
                }
            }
        }

        public void ChangeThemeTo(ElementTheme Theme)
        {
            this.Theme = Theme;
            ApplicationData.Current.LocalSettings.Values["AppFontColorMode"] = Enum.GetName(typeof(ElementTheme), Theme);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Theme)));
        }

        public AppThemeController()
        {
            if (ApplicationData.Current.LocalSettings.Values["AppFontColorMode"] is string Mode)
            {
                Theme = (ElementTheme)Enum.Parse(typeof(ElementTheme), Mode);
            }
            else
            {
                Theme = ElementTheme.Dark;
                ApplicationData.Current.LocalSettings.Values["AppFontColorMode"] = "Dark";
            }
        }
    }
    #endregion

    #region 错误追踪器
    public static class ExceptionTracer
    {
        private static AutoResetEvent Locker = new AutoResetEvent(true);

        public static void RequestBlueScreen(Exception e)
        {
            if (!(Window.Current.Content is Frame rootFrame))
            {
                rootFrame = new Frame();

                Window.Current.Content = rootFrame;
            }

            if (GlobalizationPreferences.Languages.FirstOrDefault().StartsWith("zh"))
            {
                string Message =
                "\r\r以下是错误信息：\r\rException Code错误代码：" + e.HResult +
                "\r\rMessage错误消息：" + e.Message +
                "\r\rSource来源：" + (string.IsNullOrEmpty(e.Source) ? "Unknown" : e.Source) +
                "\r\rStackTrace堆栈追踪：\r" + (string.IsNullOrEmpty(e.StackTrace) ? "Unknown" : e.StackTrace);

                rootFrame.Navigate(typeof(BlueScreen), Message);
            }
            else
            {
                string Message =
                "\r\rThe following is the error message：\r\rException Code：" + e.HResult +
                "\r\rMessage：" + e.Message +
                "\r\rSource：" + (string.IsNullOrEmpty(e.Source) ? "Unknown" : e.Source) +
                "\r\rStackTrace：\r" + (string.IsNullOrEmpty(e.StackTrace) ? "Unknown" : e.StackTrace);

                rootFrame.Navigate(typeof(BlueScreen), Message);
            }
        }

        public static async Task LogAsync(Exception Ex)
        {
            await LogAsync(Ex.Message + Environment.NewLine + Ex.StackTrace);
        }

        public static async Task LogAsync(string Message)
        {
            await Task.Run(() =>
            {
                Locker.WaitOne();
            });

            StorageFile TempFile = await ApplicationData.Current.LocalCacheFolder.CreateFileAsync("RX_Error_Message.txt", CreationCollisionOption.OpenIfExists);
            await FileIO.AppendTextAsync(TempFile, Message + Environment.NewLine);

            Locker.Set();
        }
    }
    #endregion
}
