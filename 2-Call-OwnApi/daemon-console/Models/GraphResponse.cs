
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

//using Microsoft.Graph;
//using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace daemon_console.Models
{
    public class GraphResponse<T>
    {
        [JsonPropertyName("value")]
        public List<T> Value { get; set; }
    }
}