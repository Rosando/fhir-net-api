﻿/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/ewoutkramer/fhir-net-api/master/LICENSE
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Support;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Hl7.Fhir.Rest
{
	/*
	 * Brian 16 Dec 2014:
	 *		Removed the Category in the HTML Header as we don't do this anymore for DSTU2
	 *		Implement everything using the native .net async patterns
	 */
    public static class HttpUtil
    {
        #region << HTTP Headers >>
        /// <summary>
        /// "Content-Location" found in the HTTP Headers
        /// </summary>
        public const string CONTENTLOCATION = "Content-Location";
        /// <summary>
        /// "Location" found in the HTTP Headers
        /// </summary>
        public const string LOCATION = "Location";
        /// <summary>
        /// "Last-Modified" found in the HTTP Headers
        /// </summary>
        public const string LASTMODIFIED = "Last-Modified";
        /// <summary>
        /// "ETag" found in the HTTP Headers
        /// </summary>
        public const string ETAG = "ETag";
        #endregion

        /// <summary>
        /// "_format" found as a parameter on the REST URL
        /// </summary>
        public const string RESTPARAM_FORMAT = "_format";

        /// <summary>
        /// "_since" found as a parameter on the REST History operation URL
        /// </summary>
        public const string HISTORY_PARAM_SINCE = "_since";
        /// <summary>
        /// "_count" found as a parameter on the REST History operation URL
        /// </summary>
        public const string HISTORY_PARAM_COUNT = SearchParams.SEARCH_PARAM_COUNT;

        public static async Task<byte[]> ReadAllFromStream(Stream s, int contentLength)
        {
            if (contentLength == 0)
                return null;

            //int bufferSize = contentLength < 4096 ? contentLength : 4096;
            int bufferSize = 4096;

            byte[] byteBuffer = new byte[bufferSize];
            MemoryStream buffer = new MemoryStream();

            int readLen = await s.ReadAsync(byteBuffer, 0, byteBuffer.Length);

            while (readLen > 0)
            {
                await buffer.WriteAsync(byteBuffer, 0, readLen);
                readLen = await s.ReadAsync (byteBuffer, 0, byteBuffer.Length);
            }

            return buffer.ToArray();
        }


        public static Uri MakeAbsoluteToBase(Uri location, Uri baseUrl)
        {
            // If called without a location, just return the base endpoint
            if (location == null) return baseUrl;

            // If the location is absolute, verify whether it is within the endpoint
            if (location.IsAbsoluteUri)
            {
                if (!new RestUrl(baseUrl).IsEndpointFor(location))
                    throw Error.Argument("location", "Url is not located within the given base endpoint");
            }
            else
            {
                // Else, make location absolute within the endpoint
                //location = new Uri(Endpoint, location);
                location = new RestUrl(baseUrl).AddPath(location).Uri;
            }

            return location;
        }

        public static bool IsWithin(this Uri me, Uri other)
        {
            if (!other.IsAbsoluteUri)
                return false;     // can never be within a relative path

            if (me.IsAbsoluteUri)
            {
                if (other.Authority.ToLower() != me.Authority.ToLower()) 
                    return false;
            }

            var meSegments = me.OriginalString.TrimEnd('/').ToLower().Split('/');
            var otherSegments = other.OriginalString.TrimEnd('/').ToLower().Split('/');

            var otherLength = otherSegments.Length;
            var meLength = meSegments.Length;

            if (meSegments.Length < otherSegments.Length) 
                return false;
            for (int index = 0; index < otherLength; index++)
            {
                if (otherSegments[index].TrimEnd('/') != meSegments[index].TrimEnd('/')) 
                    return false;
            }

            return true;
        }

    }
}