﻿using Microsoft.Extensions.Options;

namespace MemoryMQ;

public class MemoryMQOptionsValidation:IValidateOptions<MemoryMQOptions>
{
    public ValidateOptionsResult Validate(string? name, MemoryMQOptions options)
    {
        if (options.EnablePersistent && string.IsNullOrWhiteSpace(options.DbConnectionString))
            return ValidateOptionsResult.Fail("EnablePersistent is true, but DbConnectionString is null or empty");
        
        if(options.GlobalMaxChannelSize<=0)
            return ValidateOptionsResult.Fail("GlobalMaxChannelSize must be greater than 0");
        
        return ValidateOptionsResult.Success;
    }
}