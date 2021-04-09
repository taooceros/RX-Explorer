﻿using System;

namespace RX_Explorer.Class
{
    public class OperationListCompressionModel : OperationListBaseModel
    {
        public override string OperationKindText
        {
            get
            {
                return Globalization.GetString("TaskList_OperationKind_Compression");
            }
        }

        public CompressionType Type { get; }

        public CompressionLevel Level { get; }

        public OperationListCompressionModel(CompressionType Type, CompressionLevel Level, string[] FromPath, string ToPath, EventHandler OnCompleted = null) : base(FromPath, ToPath, OnCompleted)
        {
            this.Type = Type;
            this.Level = Level;
        }
    }
}