﻿namespace MonEndoVue.Server.Models;

public class TokenRequest
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
}