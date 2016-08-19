using EasyHttp.Http;
using EasyHttp.Infrastructure;
using JsonFx.Json;
using YouTrackSharp.Infrastructure;

namespace YouTrackSharp.Reports
{
    public class ReportsManagement : IReportsManagement
    {
        readonly IConnection _connection;

        public ReportsManagement(IConnection connection)
        {
            _connection = connection;
        }

        public Report GetReport(string id)
        {
            return _connection.Get<Report>(string.Format("current/reports/{0}?fields=reportData,oldData", id));
        }

        public string CreateReport(CreateReport report)
        {
            if (!_connection.IsAuthenticated)
            {
                throw new InvalidRequestException(Language.YouTrackClient_CreateIssue_Not_Logged_In);
            }
            try
            {
                var writer = new JsonWriter();
                string json = writer.Write(report);
                var response = _connection.Post("current/reports", json, HttpContentTypes.ApplicationJson);
                return response.id;
            }
            catch (HttpException httpException)
            {
                throw new InvalidRequestException(httpException.StatusDescription, httpException);
            }
        }

        public void Delete(string id)
        {
            _connection.Delete(string.Format("current/reports/{0}", id));
        }
    }
}
