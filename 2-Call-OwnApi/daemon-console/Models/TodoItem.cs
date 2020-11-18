// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

//using Microsoft.Graph;
//using Newtonsoft.Json.Linq;

using System.Text.Json.Serialization;

namespace daemon_console.Models
{
    public class TodoItem
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("task")]
        public string Task { get; set; }
    }
}