using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OCR_AccessControl.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using System.Drawing.Printing;

namespace OCR_AccessControl.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ILogger<DashboardController> _logger;
        private readonly ApplicationDbContext _context;

        public DashboardController(ILogger<DashboardController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // Dashboard Index (Main Dashboard View)
        public ActionResult Index()
        {
            TimeZoneInfo phtZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila");
            DateTime nowPHT = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, phtZone);

            // Current Date/Time in PHT
            ViewBag.CurrentPHT = nowPHT.ToString("MMMM dd, yyyy hh:mm tt", CultureInfo.InvariantCulture);

            // Time ranges
            DateTime todayStartPHT = nowPHT.Date;
            DateTime todayEndPHT = todayStartPHT.AddDays(1);

            DateTime weekStartPHT = todayStartPHT.AddDays(-(int)todayStartPHT.DayOfWeek + 1); // Monday-based week
            DateTime weekEndPHT = weekStartPHT.AddDays(7);

            DateTime monthStartPHT = new DateTime(nowPHT.Year, nowPHT.Month, 1);
            DateTime monthEndPHT = monthStartPHT.AddMonths(1);

            var baseQuery = _context.NonResidentLogs.AsQueryable();

            ViewBag.NonResidentCountToday = ApplyDateFilter(baseQuery, todayStartPHT, todayEndPHT, phtZone).Count();
            ViewBag.NonResidentCountWeekly = ApplyDateFilter(baseQuery, weekStartPHT, weekEndPHT, phtZone).Count();
            ViewBag.NonResidentCountMonthly = ApplyDateFilter(baseQuery, monthStartPHT, monthEndPHT, phtZone).Count();
            ViewBag.UserCount = _context.Users.Count();

            return View();
        }

        // Define ApplyDateFilter as a private helper method
        private IQueryable<NonResidentLogs> ApplyDateFilter(
            IQueryable<NonResidentLogs> query,
            DateTime startPh,
            DateTime endPh,
            TimeZoneInfo phTimeZone)
        {
            var startUtc = TimeZoneInfo.ConvertTimeToUtc(startPh, phTimeZone);
            var endUtc = TimeZoneInfo.ConvertTimeToUtc(endPh, phTimeZone);
            return query.Where(n => n.entry_time >= startUtc && n.entry_time < endUtc);
        }

        // Entries View (Full Table)
        public async Task<IActionResult> Entries(
            string filter = "all",
            string search = "",
            string status = "",
            string sort = "entry_desc",
            int pageNumber = 1,
            int pageSize = 20,
            string month = "",
            int year = 0,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var phTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila");
            var query = _context.NonResidentLogs.AsQueryable();
            var utcNow = DateTime.UtcNow;
            var nowInPh = TimeZoneInfo.ConvertTimeFromUtc(utcNow, phTimeZone);
            filter = filter?.ToLower();

            // Date Filter
            switch (filter.ToLower())
            {
                case "today":
                    var todayStartPh = nowInPh.Date;
                    var todayEndPh = todayStartPh.AddDays(1);
                    var todayStartUtc = TimeZoneInfo.ConvertTimeToUtc(todayStartPh, phTimeZone);
                    var todayEndUtc = TimeZoneInfo.ConvertTimeToUtc(todayEndPh, phTimeZone);
                    query = query.Where(n => n.entry_time >= todayStartUtc && n.entry_time < todayEndUtc);
                    break;

                case "week":
                    var weekStartPh = nowInPh.Date.AddDays(-(int)nowInPh.DayOfWeek);
                    var weekStartUtc = TimeZoneInfo.ConvertTimeToUtc(weekStartPh, phTimeZone);
                    query = query.Where(n => n.entry_time >= weekStartUtc);
                    break;

                case "month":
                    var monthStartPh = new DateTime(nowInPh.Year, nowInPh.Month, 1);
                    var monthStartUtc = TimeZoneInfo.ConvertTimeToUtc(monthStartPh, phTimeZone);
                    query = query.Where(n => n.entry_time >= monthStartUtc);
                    break;

                case "quarter":
                    var quarter = (nowInPh.Month - 1) / 3 + 1;
                    var quarterStart = new DateTime(nowInPh.Year, (quarter - 1) * 3 + 1, 1);
                    var quarterEnd = quarterStart.AddMonths(3);
                    query = ApplyDateFilter(query, quarterStart, quarterEnd, phTimeZone);
                    break;

                case "semi-annual":
                    var half = nowInPh.Month <= 6 ? 1 : 2;
                    var semiStart = new DateTime(nowInPh.Year, (half - 1) * 6 + 1, 1);
                    var semiEnd = semiStart.AddMonths(6);
                    query = ApplyDateFilter(query, semiStart, semiEnd, phTimeZone);
                    break;

                case "year":
                    var yearStart = new DateTime(nowInPh.Year, 1, 1);
                    var yearEnd = yearStart.AddYears(1);
                    query = ApplyDateFilter(query, yearStart, yearEnd, phTimeZone);
                    break;

                case "all":
                    // Explicitly handle "all" to clear date filters
                    break;

                case "range":
                    if (startDate.HasValue && endDate.HasValue)
                    {
                        // Apply the date range filter
                        query = ApplyDateFilter(query, startDate.Value, endDate.Value, phTimeZone);
                    }
                    break;

                default:
                    // No filter applied (default behavior)
                    break;
            }

            // Search Filter
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(n =>
                    n.full_name.Contains(search) ||
                    n.id_number.Contains(search) ||
                    n.id_type.Contains(search)
                );
            }

            // Status Filter
            switch (status.ToLower())
            {
                case "inside":
                    query = query.Where(n => n.exit_time == null);
                    break;
                case "exited":
                    query = query.Where(n => n.exit_time != null);
                    break;
                case "overdue":
                    var cutoffTime = DateTime.UtcNow.AddHours(-24);
                    query = query.Where(n => n.exit_time == null
                        && n.entry_time < cutoffTime);
                    break;

            }

            // Sorting
            query = sort switch
            {
                "entry_asc" => query.OrderBy(n => n.entry_time),
                "name_asc" => query.OrderBy(n => n.full_name),
                "name_desc" => query.OrderByDescending(n => n.full_name),
                _ => query.OrderByDescending(n => n.entry_time),
            };

            // Pagination
            var paginatedList = PaginatedList<NonResidentLogs>.Create(
                query,
                pageNumber,
                pageSize
            );
            // Preserve filter state with correct casing
            // In your Entries action method:

            // ... (existing filter handling code)

            // Preserve filter state with correct casing
            ViewBag.Filter = filter.ToLower() switch
            {
                "all" => "All",
                "today" => "Today",
                "week" => "Week",
                "month" => "Month",
                "quarter" => "Quarter",
                "semi-annual" => "Semi-Annual",
                "year" => "Year",
                "range" => "Range",
                _ => "All"
            };

            // Preserve date parameters (ADD THESE LINES)
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

            // Also preserve other parameters
            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.Sort = sort;

            return View(paginatedList);
        }

        // Time In View
        public async Task<IActionResult> Timein(int pageNumber = 1, int pageSize = 20)
        {
            var philippineTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila");

            var query = _context.NonResidentLogs
                .OrderByDescending(v => v.entry_time) // Add this line
                .AsQueryable()
                .Select(v => new NonResidentLogs
                {
                    id = v.id,
                    full_name = v.full_name,
                    id_type = v.id_type,
                    id_number = v.id_number,
                    entry_time = v.entry_time.HasValue
                        ? TimeZoneInfo.ConvertTimeFromUtc(v.entry_time.Value, philippineTimeZone)
                        : (DateTime?)null,
                    exit_time = v.exit_time.HasValue
                        ? TimeZoneInfo.ConvertTimeFromUtc(v.exit_time.Value, philippineTimeZone)
                        : (DateTime?)null,
                    qr_code = v.qr_code
                });

            // Apply pagination
            // Pagination
            var paginatedList = PaginatedList<NonResidentLogs>.Create(
                query,
                pageNumber,
                pageSize
            );

            return View(paginatedList);
        }

        // Time Out View
        public async Task<IActionResult> TimeOut(int pageNumber = 1, int pageSize = 20)
        {
            var philippineTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila");

            var query = _context.NonResidentLogs
                .Where(v => v.exit_time != null && v.entry_time != null) // Ensure both times exist
                .OrderByDescending(v => v.exit_time)
                .Select(v => new NonResidentLogs
                {
                    id = v.id,
                    full_name = v.full_name,
                    id_type = v.id_type,
                    id_number = v.id_number,
                    exit_time = TimeZoneInfo.ConvertTimeFromUtc(v.exit_time.Value, philippineTimeZone),
                    entry_time = TimeZoneInfo.ConvertTimeFromUtc(v.entry_time.Value, philippineTimeZone),
                    qr_code = v.qr_code
                });

            var paginatedList = PaginatedList<NonResidentLogs>.Create(query, pageNumber, pageSize);
            return View(paginatedList);
        }

        // Overdues View
        // Overdues View
        public async Task<IActionResult> Overdues(int pageNumber = 1, int pageSize = 20)
        {
            var overdueHours = 24; // Corrected syntax
            var philippineTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila");

            var query = _context.NonResidentLogs
                .Where(v => v.entry_time.HasValue &&
                            v.exit_time == null &&
                            (DateTime.UtcNow - v.entry_time.Value).TotalHours > overdueHours)
                .OrderByDescending(v => v.exit_time)
                .Select(v => new NonResidentLogs
                {
                    id = v.id,
                    full_name = v.full_name,
                    id_type = v.id_type,
                    id_number = v.id_number,
                    entry_time = v.entry_time,
                    exit_time = v.exit_time,
                    qr_code = v.qr_code
                });

            // Execute pagination directly on IQueryable
            var paginatedList = await PaginatedList<NonResidentLogs>.CreateAsync(query, pageNumber, pageSize);

            // Apply timezone conversion AFTER retrieving paginated data
            foreach (var item in paginatedList)
            {
                item.entry_time = item.entry_time.HasValue
                    ? TimeZoneInfo.ConvertTimeFromUtc(item.entry_time.Value, philippineTimeZone)
                    : null;

                item.exit_time = item.exit_time.HasValue
                    ? TimeZoneInfo.ConvertTimeFromUtc(item.exit_time.Value, philippineTimeZone)
                    : null;
            }

            return View(paginatedList);
        }


    }

}