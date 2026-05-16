using System;
using System.Collections.Generic;
using System.Text;

namespace SoftUmar_Virtuals.Application.Common
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }

        public string? Message { get; set; }

        public int StatusCode { get; set; }

        public T? Data { get; set; }

        public List<string>? Errors { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public static ApiResponse<T> SuccessResponse(
            T data,
            string message = "Request successful",
            int statusCode = 200)
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                StatusCode = statusCode,
                Data = data
            };
        }

        public static ApiResponse<T> FailureResponse(
            List<string> errors,
            string message = "Request failed",
            int statusCode = 400)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                StatusCode = statusCode,
                Errors = errors
            };
        }
    }
}
