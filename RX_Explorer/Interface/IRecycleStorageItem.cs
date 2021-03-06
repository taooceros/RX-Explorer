﻿using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace RX_Explorer.Interface
{
    public interface IRecycleStorageItem : IStorageItemPropertiesBase, INotifyPropertyChanged
    {
        public string OriginPath { get; }
        public string Size { get; }
        public string ModifiedTime { get; }
        public void SetRelatedData(string OriginPath, DateTimeOffset DeleteTime);
        public Task<bool> DeleteAsync();
        public Task<bool> RestoreAsync();
    }
}
