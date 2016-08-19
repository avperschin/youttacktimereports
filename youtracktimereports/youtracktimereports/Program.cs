using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using YouTrackSharp.Admin;
using YouTrackSharp.Infrastructure;
using YouTrackSharp.Projects;
using YouTrackSharp.Reports;

namespace youtracktimereports
{
    class Program : IDisposable
    {
        class RestartException : Exception
        {
            public RestartException() : base()
            {
            }
            public RestartException(string message) : base(message)
            {
            }
            public RestartException(string message, Exception innerException) : base(message, innerException)
            {
            }
            protected RestartException(SerializationInfo info, StreamingContext context) : base(info, context)
            {
            }
        }

        private static string YouTrackServer;
        private static string Login;
        private static string Password;
        private static bool Error;
        private static bool restartExceptionThrown;
        private static string ProjectName;
        private static string ProjectShortName;
        private static string DateStart;
        private static string DateEnd;
        private static DateTime outputStartDate;
        private static DateTime outputEndDate;
        static int Main(string[] args)
        {
            int rc = 0;

            do
            {
                restartExceptionThrown = false;
                try
                {
                    using (Program appInstance = new Program(args))
                    {
                        rc = appInstance.Execute();
                    }
                }
                catch (RestartException)
                {
                    restartExceptionThrown = true;
                }
            } while (restartExceptionThrown);
            return rc;
        }
        public Program(string[] args)
        {
            Console.Clear();
            //При запуске приложения, если значения параметров приложения пусты - просим пользователя ввести их.
            //При заполненных значениях, пользователю всеравно предлагается ввести их, но в квадратных скобках показывается уже сохраненный вариант, для пропуска просто нажать Enter
            YouTrackServer = GetSetting("YouTrackServer");
            if (string.IsNullOrEmpty(YouTrackServer))
            {
                Console.Write("Введите адрес сервера (ваш_сервер.myjetbrains.com): ");
                YouTrackServer = Console.ReadLine();
                SetSetting("YouTrackServer", YouTrackServer);
            }
            else
            {
                Console.Write(String.Format("Введите адрес сервера [{0}]: ", YouTrackServer));
                string temp = Console.ReadLine();
                if (!string.IsNullOrEmpty(temp) && YouTrackServer != temp)
                {
                    YouTrackServer = temp;
                    SetSetting("YouTrackServer", YouTrackServer);
                }
            }
            Login = GetSetting("Login");
            if (string.IsNullOrEmpty(Login))
            {
                Console.Write("Введите имя пользователя: ");
                Login = Console.ReadLine();
                SetSetting("Login", Login);
            }
            else
            {
                Console.Write(string.Format("Введите имя пользователя [{0}]: ", Login));
                string temp = Console.ReadLine();
                if (!string.IsNullOrEmpty(temp) && Login != temp)
                {
                    Login = temp;
                    SetSetting("Login", Login);
                }
            }
            Password = GetSetting("Password");
            if (string.IsNullOrEmpty(Password))
            {
                Console.Write("Введите пароль: ");
                Password = Console.ReadLine();
                SetSetting("Password", Password);
            }
            else
            {
                Console.Write(string.Format("Введите пароль [{0}]: ", Password));
                string temp = Console.ReadLine();
                if (!string.IsNullOrEmpty(temp) && Password != temp)
                {
                    Password = temp;
                    SetSetting("Password", Password);
                }
            }
            //Параметры ниже не сохраняются, при ошибочном вводе, требуется ввести заново в правильном формате
            Console.Write("Введите дату начала отчета (01.01.2001): ");
            DateStart = Console.ReadLine();
            Error = true;
            while (Error)
            {
                if (DateTime.TryParseExact(DateStart + " 00:00:00", "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out outputStartDate))
                {
                    Error = false;
                }
                else
                {
                    Console.Write("Введите правильный формат даты начала отчета (01.01.2001): ");
                    DateStart = Console.ReadLine();
                    Error = true;
                }
            }
            Console.Write("Введите дату окончания отчета (01.01.2001): ");
            DateEnd = Console.ReadLine();
            Error = true;
            while (Error)
            {
                if (DateTime.TryParseExact(DateEnd + " 23:59:59", "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out outputEndDate))
                {
                    Error = false;
                }
                else
                {
                    Console.Write("Введите правильный формат даты окончания отчета (01.01.2001): ");
                    DateStart = Console.ReadLine();
                    Error = true;
                }
            }
        }

        public int Execute()
        {
            try
            {
                List<Report> reports = new List<Report>();
                bool IsSsl = false;
                if (GetSetting("IsSsl") == "y") IsSsl = true;
                //Инициализируем подключение к серверу. Параметры порт, SSL и путь можно изменить в настройках приложения в файле App.config в секции AppSettings
                var connection = new Connection(YouTrackServer, Convert.ToInt32(GetSetting("Port")), IsSsl, GetSetting("Path"));
                //Авторизируемся на сервере
                connection.Authenticate(Login, Password);
                Error = true;
                while (Error)
                {
                    //Проверяем существует ли проект
                    string ProjectName = CheckProject(connection);
                    if (ProjectName == "PROJECT_ERROR")
                    {
                        if(!string.IsNullOrEmpty(ProjectShortName)) Console.WriteLine("Произошла ошибка! Проект с таким ID не найден!");
                        Error = true;
                        Console.Write("Введите ID проекта: ");
                        ProjectShortName = Console.ReadLine();
                    }
                    else
                    {
                        Console.WriteLine("Сейчас начнется  формирование отчета для проекта: " + ProjectName);
                        Error = false;
                    }
                }
                if (!Error)
                {
                    TimeSpan oneDay = TimeSpan.FromDays(1);
                    int i = 0;
                    List<DateTime> dateList = new List<DateTime>();
                    //Цикл получения результатов отчета по заданному периоду. Из-за их АПИ приходится создавать отчет по каждой дате переода.
                    for (DateTime date = outputStartDate; date <= outputEndDate; date += oneDay)
                    {
                        long ds = UnixTimeStampUTC(date);
                        long de = UnixTimeStampUTC(date.AddHours(23).AddMinutes(59).AddSeconds(59));
                        string reportName = "TimeReport_" + i;
                        var reportsManagement = new ReportsManagement(connection);
                        //Формируем создаваемый отчет с параметрами: фиксированный, за определенную дату, для заданного проекта с группировкой по пользователям
                        var createReport = new CreateReport() { type = "time", parameters = new CreateReportParameters() { range = new Range() { type = "fixed", from = ds, to = de }, projects = new List<Projects>() { new Projects() { name = ProjectName, shortName = ProjectShortName } }, groupById = "WORK_AUTHOR" }, own = true, name = reportName };
                        //Отправляем создаваемый отчет на сервер, в ответ получаем ID отчета
                        string reportId = reportsManagement.CreateReport(createReport);
                        //Получаем результаты отчета с сервера
                        var report = reportsManagement.GetReport(reportId);
                        //Добавляем дополнительные значения в отчет
                        report.reportDate = date;
                        if (report.reportData.groups.Count > 0) report.reportFill = true;
                        //Добавляем отчет в список отчетов
                        reports.Add(report);
                        //Удаляем отчет с сервера
                        reportsManagement.Delete(reportId);
                        dateList.Add(date);
                        i++;
                    }
                    //Проверяем пустые отчеты или нет
                    if (reports.Where(r => r.reportFill == true).ToList().Count > 0)
                    {
                        //Формируем файл CSV
                        string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments);
                        string fileDate = DateTime.Now.ToString("yyyyMMddHHmmss");
                        string file = Path.Combine(documentsPath, "TimeReport_" + fileDate + ".csv");
                        //Получаем список пользователей с сервера
                        //Я не знаю как у Вас на сервере настроены группы и роли для пользователей, сейчас список пользователей по идее должен 
                        //браться из участников заданного проекта, но то ли я не правильно проект или сервер настроил, то ли у авторов чет не то, 
                        //в общем если выбираем пользователей из проекта, тянутся всеравно все пользователи, т.к. они в группе All Users
                        //, а эта группа по умолчанию имеет доступ к любому проекту. Есть вариант отсеивать пользователей по группе
                        //, но я не знаю как у Вас настроены группы, поэтому пока беру всех.
                        UserManagement userManagement = new UserManagement(connection);
                        var users = userManagement.GetUsers(null, null, null, ProjectShortName, null, null, null);
                        //Получаем настройки тайм трекинга с сервера
                        var timesettings = userManagement.GetTimeSettings();
                        List<csvReport> csvreports = new List<csvReport>();
                        //Подготовительный цикл формирования окончательного отчета, в нем заполняем поля на основе списка пользователей и настроек тайм трекинга
                        foreach (var user in users)
                        {
                            csvReport csvreport = new csvReport();
                            csvreport.UserName = user.FullName;
                            csvreport.UserLogin = user.Username;
                            csvreport.WorkingDays = new List<WorkingDay>();
                            dateList.ForEach(date =>
                            {
                                WorkingDay workingDay = new WorkingDay();
                                workingDay.Date = date;
                                workingDay.Duration = 0;
                                workingDay.Estimation = 0;
                                workingDay.Norm = 0;
                                timesettings.WorkWeek.ForEach(workDay =>
                                {
                                    if(workDay.Value.ToString().ToLower() == date.DayOfWeek.ToString().ToLower()) workingDay.Norm += timesettings.HoursADay;
                                });
                                workingDay.WeekNumber = GetWeekNumber(date);
                                csvreport.WorkingDays.Add(workingDay);
                            });
                            csvreports.Add(csvreport);
                        }
                        using (StreamWriter stream = new StreamWriter(File.Open(file, FileMode.Create), Encoding.Default))
                        {
                            //Заполняем первую строку (заголовки) CSV файла
                            string col1 = "Имя";
                            string col2 = "Запланировано часов";
                            string col3 = "Норма";
                            string col4 = "Общее кол-во часов";
                            string csvRow = string.Format("{0};{1};{2};{3}", col1, col2, col3, col4);
                            dateList.ForEach(date =>
                            {
                                csvRow += string.Format(";{0}", date.ToShortDateString());
                            });
                            stream.WriteLine(csvRow);
                            //Второй цикл формирования окончательного отчета
                            csvreports.ForEach(csvreport =>
                            {
                                //Третий цикл формирования окончательного отчета, в нем заполняем для каждого пользователя данные о запланированных часах и отработанных часах
                                csvreport.WorkingDays.ForEach(day =>
                                {
                                    var report = reports.Where(r => r.reportDate == day.Date).FirstOrDefault();
                                    if(report != null)
                                    {
                                        var group = report.reportData.groups.Where(g => g.name == csvreport.GroupName).FirstOrDefault();
                                        if(group != null)
                                        {
                                            day.Duration = group.durationNumber;
                                            if (group.durationNumber == 0)
                                            {
                                                day.Duration = group.lines.Sum(l => l.durationNumber);
                                            }
                                            day.Estimation = group.estimationNumber;
                                            if (group.estimationNumber == 0)
                                            {
                                                day.Estimation = group.lines.Sum(l => l.estimationNumber);
                                            }
                                        }
                                    }
                                });
                                //Создаем строку в CSV файле для каждого пользователя в цикле, высчитываем сумму запланированных и отработанных часов, а так же процент выполненной нормы на основе выбранного периода
                                col1 = csvreport.UserName;
                                double sum = csvreport.WorkingDays.Sum(d => d.Duration);
                                int sumNorm = csvreport.WorkingDays.Sum(d => d.Norm);
                                double Norm = (sum * 100) / sumNorm;
                                double sumPlan = csvreport.WorkingDays.Sum(d => d.Estimation);
                                col2 = sumPlan.ToString("N1");
                                col3 = Norm.ToString("N1") + "%";
                                col4 = sum.ToString("N1");
                                csvRow = string.Format("{0};{1};{2};{3}", col1, col2, col3, col4);
                                csvreport.WorkingDays.ForEach(day =>
                                {
                                    if (day.Duration > 0)
                                    {
                                        csvRow += string.Format(";{0}", day.Duration.ToString("N1"));
                                    }
                                    else
                                    {
                                        csvRow += ";";
                                    }
                                });
                                stream.WriteLine(csvRow);
                            });
                            //Создаем заключительные строки в таблице CSV
                            int count = 0;
                            csvRow = "Всего часов ежедневно;;;";
                            string csvRowWeek = "Всего часов еженедельно;;;";
                            string csvRowPlan = "Всего запланированных часов;;;";
                            string csvRowPlanWeek = "Всего запланированных часов еженедельно;;;";
                            int WorkingDays = 0;
                            double PlanedToday = 0;
                            double PlanedPeriod = 0;
                            double WorkToday = 0;
                            double WorkPeriod = 0;
                            var workingDays = csvreports.SelectMany(r => r.WorkingDays);
                            dateList.ForEach(date =>
                            {
                                DateTime now = DateTime.Now;
                                timesettings.WorkWeek.ForEach(workDay =>
                                {
                                    if (workDay.Value.ToString().ToLower() == date.DayOfWeek.ToString().ToLower()) WorkingDays += 1;
                                });
                                count++;
                                double sumDay = workingDays.Where(d => d.Date == date).Sum(l => l.Duration);
                                double sumPlan = workingDays.Where(d => d.Date == date).Sum(l => l.Estimation);
                                if (date == now)
                                {
                                    WorkToday = sumDay;
                                    PlanedToday = sumPlan;
                                }
                                if(sumDay > 0)
                                {
                                    csvRow += string.Format(";{0}", sumDay.ToString("N1"));
                                }
                                else
                                {
                                    csvRow += ";";
                                }
                                if (sumPlan > 0)
                                {
                                    csvRowPlan += string.Format(";{0}", sumPlan.ToString("N1"));
                                }
                                else
                                {
                                    csvRowPlan += ";";
                                }
                                int week = GetWeekNumber(date);
                                var Days = workingDays.Where(d => d.WeekNumber == week);
                                double sumWeek = Days.Sum(l => l.Duration);
                                double sumPlanWeek = Days.Sum(l => l.Estimation);
                                int gcnt = Days.GroupBy(d => d.Date).Count();
                                if (count < gcnt)
                                {
                                    csvRowWeek += ";";
                                    csvRowPlanWeek += ";";
                                }
                                else if (count == gcnt)
                                {
                                    count = 0;
                                    if (sumWeek > 0)
                                    {
                                        csvRowWeek += string.Format(";{0}", sumWeek.ToString("N1"));
                                    }
                                    else
                                    {
                                        csvRowWeek += ";";
                                    }
                                    if (sumPlanWeek > 0)
                                    {
                                        csvRowPlanWeek += string.Format(";{0}", sumPlanWeek.ToString("N1"));
                                    }
                                    else
                                    {
                                        csvRowPlanWeek += ";";
                                    }
                                }
                            });
                            stream.WriteLine(csvRow);
                            stream.WriteLine(csvRowWeek);
                            stream.WriteLine(csvRowPlan);
                            stream.WriteLine(csvRowPlanWeek);
                            //Создаем итоговые строки в файле CSV
                            int colCount = 4 + dateList.Count;
                            csvRow = "";
                            string csvRowHours = "";
                            string csvRowPlaned = "";
                            string csvRowNorm = "";
                            for (int g = 0; g < colCount; g++)
                            {
                                if (g + 1 == colCount - 1)
                                {
                                    csvRow += ";Период";
                                    WorkPeriod = workingDays.Sum(d => d.Duration);
                                    csvRowHours += string.Format(";{0}", WorkPeriod.ToString("N1"));
                                    PlanedPeriod = workingDays.Sum(d => d.Estimation);
                                    csvRowPlaned += string.Format(";{0}", PlanedPeriod.ToString("N1"));
                                    int Weeks = workingDays.GroupBy(w => w.WeekNumber).Count();
                                    int weekDays = Weeks * timesettings.DaysAWeek;
                                    if (Weeks == 1)
                                    {
                                        weekDays = WorkingDays;
                                    }
                                    double normHours = timesettings.HoursADay * weekDays * csvreports.Count ;
                                    csvRowNorm += string.Format(";{0}", normHours.ToString("N1"));
                                }
                                else if (g + 1 == colCount - 2)
                                {
                                    csvRow += ";Сегодня";
                                    csvRowHours += string.Format(";{0}", WorkToday.ToString("N1"));
                                    csvRowPlaned += string.Format(";{0}", PlanedToday.ToString("N1"));
                                    double normHours = timesettings.HoursADay * csvreports.Count;
                                    csvRowNorm += string.Format(";{0}", normHours.ToString("N1"));
                                }
                                else if (g + 1 == colCount - 3)
                                {
                                    csvRow += ";Всего";
                                    csvRowHours += ";Отработанные";
                                    csvRowPlaned += ";Запланированные";
                                    csvRowNorm += ";Требуемые";
                                }
                                else
                                {
                                    csvRow += ";";
                                    csvRowHours += ";";
                                    csvRowPlaned += ";";
                                    csvRowNorm += ";";
                                }
                            }
                            stream.WriteLine(csvRow);
                            stream.WriteLine(csvRowHours);
                            stream.WriteLine(csvRowPlaned);
                            stream.WriteLine(csvRowNorm);
                        }
                        Console.WriteLine("Файл с отчетом сохранен: " + file);
                        Console.WriteLine("Нажмите любую клавишу для выхода...");
                        Console.ReadKey();
                    }
                    else
                    {
                        Console.WriteLine("За выбранный Вами период, по данному проекту не проводилось работ!");
                        Console.WriteLine("Нажмите любую клавишу для выхода...");
                        Console.ReadKey();
                    }
                }
                connection.Logout();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Произошла ошибка подключения к серверу, проверьте правильность заполнения полей! " + ex.Message);
                Console.WriteLine("Нажмите любую клавишу для продолжения...");
                Console.ReadKey();
                throw new RestartException();
            }
            return 1;
        }

        public void Dispose()
        {
            ProjectName = null;
            ProjectShortName = null;
            DateStart = null;
            DateEnd = null;
        }
        /// <summary>
        /// Проверка существованяи проекта
        /// </summary>
        /// <param name="connection">Обязательный параметр, требуется для подключения к серверу</param>
        /// <returns>Если проект существует, возвращается строка с полным наименованием проекта, если нет - возвращается строка с ошибкой.</returns>
        private static string CheckProject(Connection connection)
        {
            try
            {
                var projectManagement = new ProjectManagement(connection);
                var p = projectManagement.GetProject(ProjectShortName);
                ProjectName = p.Name;
                return p.Name;
            }
            catch
            {
                return "PROJECT_ERROR";
            }
        }
        /// <summary>
        /// Получаем номер недели по дате
        /// </summary>
        /// <param name="date">Обязательный параметр, передается для получения номера недели</param>
        /// <returns>Возвращается число, полученного номера недели</returns>
        private static int GetWeekNumber(DateTime date)
        {
            CultureInfo ciCurr = CultureInfo.CurrentCulture;
            int weekNum = ciCurr.Calendar.GetWeekOfYear(date, CalendarWeekRule.FirstDay, DayOfWeek.Monday);
            return weekNum;
        }
        /// <summary>
        /// Конвертация даты в милисекунды
        /// </summary>
        /// <param name="date">Обязательный параметр, передается для конвертации</param>
        /// <returns>Возвращается число, дата сконвертированная в милисекунды</returns>
        private static Int64 UnixTimeStampUTC(DateTime date)
        {
            Int64 unixTimeStamp;
            DateTime zuluTime = new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second, date.Millisecond, DateTimeKind.Utc);
            DateTime unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            unixTimeStamp = (Int64)(zuluTime.Subtract(unixEpoch)).TotalMilliseconds;
            return unixTimeStamp;
        }
        /// <summary>
        /// Получаем значение параметра приложения
        /// </summary>
        /// <param name="key">Обязательный параметр, передается для получения значения ключа</param>
        /// <returns>Возвращается строка с полученным значением</returns>
        private static string GetSetting(string key)
        {
            return ConfigurationManager.AppSettings[key];
        }
        /// <summary>
        /// Сохраняем значение параметра приложения
        /// </summary>
        /// <param name="key">Обязательный параметр, передается для указания изменяемого параметра приложения</param>
        /// <param name="value">Обязательный параметр, передается для изменения значения параметра приложения</param>
        /// <returns></returns>
        private static void SetSetting(string key, string value)
        {
            Configuration configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            configuration.AppSettings.Settings[key].Value = value;
            configuration.Save(ConfigurationSaveMode.Full, true);
            ConfigurationManager.RefreshSection("appSettings");
        }
    }
}
