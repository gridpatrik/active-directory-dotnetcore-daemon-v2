// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

//using Microsoft.Graph;
//using Newtonsoft.Json.Linq;
using System;
using System.Text.Json.Serialization;

namespace daemon_console.Models
{
    public class GraphUser
    {
        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; }
        [JsonPropertyName("id")]
        public Guid Id { get; set; }
    }
}