﻿
namespace ContentMetadataServiceAPI.Models
{
    internal class ResponseModel<T>
    {
        public T Result { get; set; }
        public string Message { get; set; }
    }
}
