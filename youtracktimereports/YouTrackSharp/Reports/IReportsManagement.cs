namespace YouTrackSharp.Reports
{
    public interface IReportsManagement
    {
        Report GetReport(string id);
        string CreateReport(CreateReport report);
        void Delete(string id);
    }
}
