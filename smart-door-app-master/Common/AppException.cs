﻿namespace Common;

public class AppException : Exception
{
    public AppException(string message) : base(message) { }
}