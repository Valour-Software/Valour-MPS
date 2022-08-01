﻿using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Valour.MPS.Database;
using Valour.MPS.Media;
using Valour.Shared;

namespace Valour.MPS.Storage;

public static class BucketManager
{
    public static AmazonS3Client Client { get; set; }
    static SHA256 SHA256 = SHA256.Create();

    public static async Task<TaskResult> Upload(MemoryStream data, string extension, long userId, string mime, 
        ContentCategory category, MediaDb db)
    {
        // Get hash from image
        byte[] hashBytes = SHA256.ComputeHash(data.GetBuffer());
        string hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

        // Add file extension to the end
        hash = hash + extension;

        // Check if this same file has already been posted by this user.
        var id = $"{category}/{userId}/{hash}";

        // If so, return the location (wooo easy route)
        if (await db.BucketItems.AnyAsync(x => x.Id == id))
            return new TaskResult(true, $"https://vmps.valour.gg/content/{id}");

        // We need a bucket record no matter what at this point
        var bucketRecord = new BucketItem()
        {
            Id = id,
            Category = category,
            Hash = hash,
            MimeType = mime,
            UserId = userId
        };

        // Now we check if anyone else has already posted this file.
        // If so, we can just create a new path to the file
        if (await db.BucketItems.AnyAsync(x => x.Hash == hash))
        {
            // Alright, someone else posted this. Let's make a new route to this
            // object without actually re-uploading it.
            try
            {
                await db.BucketItems.AddAsync(bucketRecord);
                await db.SaveChangesAsync();
            }
            catch(System.Exception e)
            {
                Console.WriteLine("Critical error when adding new route to existing bucket item.");
                Console.WriteLine(e.Message);

                return new TaskResult(false, "Critical error when adding new route to existing bucket item."); 
            }

            return new TaskResult(true, $"https://vmps.valour.gg/content/{id}");
        }

        // This object is unique and has to be posted to the bucket
        PutObjectRequest request = new()
        {
            Key = hash,
            // ChecksumAlgorithm = ChecksumAlgorithm.SHA256,
            InputStream = data,
            BucketName = "valourmps",
            DisablePayloadSigning = true
        };

        var response = await Client.PutObjectAsync(request);

        if (!IsSuccessStatusCode(response.HttpStatusCode))
        {
            return new TaskResult(false, $"Failed to PUT object into bucket. ({response.HttpStatusCode})");
        }
        else
        {
            try
            {
                await db.BucketItems.AddAsync(bucketRecord);
                await db.SaveChangesAsync();
            }
            catch (System.Exception e)
            {
                Console.WriteLine("Critical error when adding route to new item.");
                Console.WriteLine(e.Message);

                return new TaskResult(false, "Critical error when adding new route to existing bucket item.");
            }

            return new TaskResult(true, $"https://vmps.valour.gg/content/{id}");
        }
    }

    public static bool IsSuccessStatusCode(HttpStatusCode statusCode)
    {
        var intStatus = (int)statusCode;
        return (intStatus >= 200) && (intStatus <= 299);
    }
}

