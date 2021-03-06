﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Search;

namespace RX_Explorer.Class
{
    public sealed class RootStorageFolder : FileSystemStorageFolder
    {
        private static RootStorageFolder instance;

        public static RootStorageFolder Instance
        {
            get
            {
                return instance ??= new RootStorageFolder();
            }
        }

        public override string Name
        {
            get
            {
                return Globalization.GetString("RootStorageFolderDisplayName");
            }
        }

        public override string DisplayName
        {
            get
            {
                return Name;
            }
        }

        protected override bool CheckIfPropertiesLoaded()
        {
            return true;
        }

        protected override Task LoadPropertiesAsync(bool ForceUpdate)
        {
            return Task.CompletedTask;
        }

        public override Task<IStorageItem> GetStorageItemAsync()
        {
            return Task.FromResult<IStorageItem>(null);
        }

        public override Task<ulong> GetFolderSizeAsync(CancellationToken CancelToken = default)
        {
            return Task.FromResult((ulong)0);
        }

        public override Task<(uint, uint)> GetFolderAndFileNumAsync(CancellationToken CancelToken = default)
        {
            return Task.FromResult(((uint)0, (uint)0));
        }

        public override Task<IReadOnlyList<FileSystemStorageItemBase>> GetChildItemsAsync(bool IncludeHiddenItems, bool IncludeSystemItem, uint MaxNumLimit = uint.MaxValue, BasicFilters Filter = BasicFilters.File | BasicFilters.Folder, Func<string, bool> AdvanceFilter = null)
        {
            return Task.FromResult<IReadOnlyList<FileSystemStorageItemBase>>(new List<FileSystemStorageItemBase>(0));
        }

        public override Task<bool> CheckContainsAnyItemAsync(bool IncludeHiddenItem = false, bool IncludeSystemItem = false, BasicFilters Filter = BasicFilters.File | BasicFilters.Folder)
        {
            return Task.FromResult(false);
        }

        public override async Task<IReadOnlyList<FileSystemStorageItemBase>> SearchAsync(string SearchWord, bool SearchInSubFolders = false, bool IncludeHiddenItem = false, bool IncludeSystemItem = false, bool IsRegexExpresstion = false, bool IgnoreCase = true, CancellationToken CancelToken = default)
        {
            foreach (DriveDataBase Drive in CommonAccessCollection.DriveList)
            {
                if (WIN_Native_API.CheckLocationAvailability(Drive.Path))
                {
                    return await Task.Factory.StartNew(() => WIN_Native_API.Search(Drive.Path, SearchWord, SearchInSubFolders, IncludeHiddenItem, IncludeSystemItem, IsRegexExpresstion, IgnoreCase, CancelToken), TaskCreationOptions.LongRunning);
                }
                else
                {
                    List<FileSystemStorageItemBase> Result = new List<FileSystemStorageItemBase>();

                    if (Drive.DriveFolder != null)
                    {
                        QueryOptions Options = new QueryOptions
                        {
                            FolderDepth = SearchInSubFolders ? FolderDepth.Deep : FolderDepth.Shallow,
                            IndexerOption = IndexerOption.DoNotUseIndexer
                        };
                        Options.SetThumbnailPrefetch(ThumbnailMode.ListView, 150, ThumbnailOptions.UseCurrentScale);
                        Options.SetPropertyPrefetch(PropertyPrefetchOptions.BasicProperties, new string[] { "System.FileName", "System.Size", "System.DateModified", "System.DateCreated" });

                        if (!IsRegexExpresstion)
                        {
                            Options.ApplicationSearchFilter = $"System.FileName:~~\"{SearchWord}\"";
                        }

                        StorageItemQueryResult Query = Drive.DriveFolder.CreateItemQueryWithOptions(Options);

                        for (uint Index = 0; !CancelToken.IsCancellationRequested; Index += 50)
                        {
                            IReadOnlyList<IStorageItem> ReadOnlyItemList = await Query.GetItemsAsync(Index, 50).AsTask(CancelToken);

                            if (ReadOnlyItemList.Count > 0)
                            {
                                foreach (IStorageItem Item in IsRegexExpresstion
                                                              ? ReadOnlyItemList.Where((Item) => Regex.IsMatch(Item.Name, SearchWord, IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None))
                                                              : ReadOnlyItemList.Where((Item) => Item.Name.Contains(SearchWord, IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)))
                                {
                                    if (CancelToken.IsCancellationRequested)
                                    {
                                        break;
                                    }

                                    switch (Item)
                                    {
                                        case StorageFolder SubFolder:
                                            {
                                                Result.Add(await CreateFromStorageItemAsync(SubFolder));
                                                break;
                                            }
                                        case StorageFile SubFile:
                                            {
                                                Result.Add(await CreateFromStorageItemAsync(SubFile));
                                                break;
                                            }
                                    }
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                    }

                    return Result;
                }
            }

            return new List<FileSystemStorageItemBase>(0);
        }

        private RootStorageFolder() : base("RootFolderUniquePath", default)
        {

        }
    }
}
