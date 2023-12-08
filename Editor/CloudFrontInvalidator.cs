using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.CloudFront;
using Amazon.CloudFront.Model;
using UnityEngine;

namespace S3_Uploader.Editor
{
    public interface IInvalidator
    {
        void InvalidateObject(string key = "/*");
    }

    public class CloudFrontInvalidator : IInvalidator
    {
        const string amazonBucketUriSuffix = ".s3.amazonaws.com";
        const string dateFormatWithMilliseconds = "yyyy-MM-dd hh:mm:ss.ff";
        readonly IAmazonCloudFront cloudFrontClient;
        private CreateInvalidationRequest pendingRequest;
        private readonly object pendingRequestLockTarget = new object();

        public CloudFrontInvalidator(IAmazonCloudFront cloudFrontClient)
        {
            this.cloudFrontClient = cloudFrontClient;
        }

        public void InvalidateObject(string key = "/*")
        {
            const string distId = "E2ZNBZTLCDIGXB";

            if (string.IsNullOrWhiteSpace(distId))
                return;
            
            lock (pendingRequestLockTarget)
            {
                if (pendingRequest == null)
                {
                    pendingRequest = new CreateInvalidationRequest()
                    {
                        DistributionId = distId,
                        InvalidationBatch = new InvalidationBatch()
                        {
                            CallerReference = DateTime.Now.ToString(dateFormatWithMilliseconds),
                            Paths = new Paths
                            {
                                Quantity = 1,
                                Items = new List<string> {key}
                            }
                        }
                    };
                }
                else
                {
                    pendingRequest.InvalidationBatch.Paths.Items.Add(key);
                }
            }
        }


        private async Task Flush()
        {
            Task<CreateInvalidationResponse> handle = null;
            lock (pendingRequestLockTarget)
            {
                if (pendingRequest != null)
                {
                    handle = cloudFrontClient.CreateInvalidationAsync(pendingRequest);
                    pendingRequest = null;
                }
            }

            if(handle != null)
                await handle;
        }

        
        public async Task SendInvalidation()
        {
            await Flush();
            cloudFrontClient.Dispose();
        }
    }
}