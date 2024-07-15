using Microsoft.Graph;
using Microsoft.Graph.Communications.Common.Telemetry;
using System.Net;
using System.Runtime.CompilerServices;

namespace EchoBot.Util
{
    /// <summary>
    /// Extension methods for Exception.
    /// </summary>
    public static class ExceptionExtensions
    {
        /// <summary>
        /// Extension for Task to execute the task in background and log any exception.
        /// </summary>
        /// <param name="task">Task to execute and capture any exceptions.</param>
        /// <param name="logger">Graph logger.</param>
        /// <param name="description">Friendly description of the task for debugging purposes.</param>
        /// <param name="memberName">Calling function.</param>
        /// <param name="filePath">File name where code is located.</param>
        /// <param name="lineNumber">Line number where code is located.</param>
        /// <returns>A <see cref="Task" /> representing the asynchronous operation.</returns>
        public static async Task ForgetAndLogExceptionAsync(
            this Task task,
            IGraphLogger logger,
            string description = null,
            [CallerMemberName] string memberName = null,
            [CallerFilePath] string filePath = null,
            [CallerLineNumber] int lineNumber = 0)
        {
            try
            {
                await task.ConfigureAwait(false);
                logger?.Verbose(
                    $"Completed running task successfully: {description ?? string.Empty}",
                    memberName: memberName,
                    filePath: filePath,
                    lineNumber: lineNumber);
            }
            catch (Exception e)
            {
                // Log and absorb all exceptions here.
                logger?.Error(
                    e,
                    $"Caught an Exception running the task: {description ?? string.Empty}",
                    memberName: memberName,
                    filePath: filePath,
                    lineNumber: lineNumber);
            }
        }
        /// <summary>
        /// Inspect the exception type/error and return the correct response.
        /// </summary>
        /// <param name="exception">The caught exception.</param>
        /// <returns>The <see cref="HttpResponseMessage" />.</returns>
        public static HttpResponseMessage InspectExceptionAndReturnResponse(this Exception exception)
        {
            HttpResponseMessage responseToReturn;
            if (exception is ServiceException e)
            {
                responseToReturn = (int)e.StatusCode >= 200
                    ? new HttpResponseMessage(e.StatusCode)
                    : new HttpResponseMessage(HttpStatusCode.InternalServerError);
                if (e.ResponseHeaders != null)
                {
                    foreach (var responseHeader in e.ResponseHeaders)
                    {
                        responseToReturn.Headers.TryAddWithoutValidation(responseHeader.Key, responseHeader.Value);
                    }
                }

                responseToReturn.Content = new StringContent(e.ToString());
            }
            else
            {
                responseToReturn = new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent(exception.ToString()),
                };
            }

            return responseToReturn;
        }
    }
}
