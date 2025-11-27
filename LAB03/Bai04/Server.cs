using Microsoft.VisualBasic.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Bai04
{
    public partial class Server : Form
    {
        private class Movie
        {
            public int Id;
            public string Name = "";
            public int BasePrice;
            public List<int> Rooms = new List<int>();
        }
        private readonly List<StreamWriter> clientStreams = new List<StreamWriter>();
        private readonly Dictionary<string, int> customerTickets =new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, HashSet<(int movieId, int room)>> customerRooms =new Dictionary<string, HashSet<(int, int)>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<int, Movie> movies = new Dictionary<int, Movie>();
        private readonly Dictionary<(int movieId, int room), HashSet<string>> sold = new Dictionary<(int, int), HashSet<string>>();
        private readonly Dictionary<int, int> revenue = new Dictionary<int, int>();
        private readonly object _lock = new object();

        private TcpListener listener;
        private Thread acceptThread;
        private volatile bool running = false;

        public Server()
        {
            InitializeComponent();
        }

        private void AppendLog(string msg)
        {
            if (lvLog.InvokeRequired)
            {
                lvLog.Invoke(new Action<string>(AppendLog), msg);
                return;
            }
            lvLog.Items.Add($"{DateTime.Now:HH:mm:ss} - {msg}");
            lvLog.TopIndex = lvLog.Items.Count - 1;
        }

        private void SetStatus(string text)
        {
            if (lblStatus.InvokeRequired)
            {
                lblStatus.Invoke(new Action<string>(SetStatus), text);
                return;
            }
            lblStatus.Text = text;
        }

        private void SetProgress(int value, int max)
        {
            if (progressBar1.InvokeRequired)
            {
                progressBar1.Invoke(new Action<int, int>(SetProgress), value, max);
                return;
            }
            progressBar1.Maximum = Math.Max(1, max);
            progressBar1.Value = Math.Min(value, progressBar1.Maximum);
        }

        private void btnLoadFile_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog
            {
                Title = "Chọn file chứa dữ liệu phim phòng",
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                InitialDirectory = Application.StartupPath,
                Multiselect = false,
                FileName = "input5.txt"
            })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    txtDataFile.Text = ofd.FileName;
                    try
                    {
                        LoadMoviesFromFile(ofd.FileName);
                        AppendLog($"Đã nạp {movies.Count} phim từ file.");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Có lỗi xảy ra, không đọc được file. " + ex.Message);
                    }
                }
            }
        }

        private void LoadMoviesFromFile(string path)
        {
            var lines = File.ReadAllLines(path, Encoding.UTF8)
                            .Select(l => l.Trim())
                            .Where(l => !string.IsNullOrWhiteSpace(l))
                            .ToList();

            if (lines.Count % 3 != 0)
            {
                throw new Exception(
                    "File dữ liệu không đúng định dạng.\n" +
                    "Mỗi phim phải gồm đúng 3 dòng: TÊN PHIM, GIÁ VÉ CHUẨN, DANH SÁCH PHÒNG.");
            }

            lock (_lock)
            {
                movies.Clear();
                sold.Clear();
                revenue.Clear();
                customerTickets.Clear();
                customerRooms.Clear();

                int lineIndex = 0;
                int id = 1;

                while (lineIndex < lines.Count)
                {
                    string name = lines[lineIndex++];
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        throw new Exception($"Tên phim trống tại block phim ID={id} (nhóm 3 dòng thứ {((id - 1) * 3 + 1)}).");
                    }
                    string priceLine = lines[lineIndex++];
                    if (!int.TryParse(priceLine, out int basePrice) || basePrice <= 0)
                    {
                        throw new Exception(
                            $"Giá vé chuẩn không hợp lệ cho phim \"{name}\" (ID={id}).\n" +
                            $" Giá phải là số nguyên dương.: \"{priceLine}\"");
                    }
                    string roomLine = lines[lineIndex++];
                    var roomTokens = roomLine.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    var roomList = new List<int>();
                    foreach (var tk in roomTokens)
                    {
                        if (!int.TryParse(tk, out int r) || r < 1 || r > 3)
                        {
                            throw new Exception(
                                $"Không được dùng phòng chiếu \"{tk}\" cho phim \"{name}\" (ID={id}).\n" +
                                "Chỉ được phép sử dụng các phòng 1, 2 hoặc 3.");
                        }

                        if (!roomList.Contains(r))
                            roomList.Add(r);
                    }

                    if (roomList.Count == 0)
                    {
                        throw new Exception($"Phim \"{name}\" (ID={id}) không có phòng chiếu hợp lệ.");
                    }

                    var m = new Movie
                    {
                        Id = id,
                        Name = name,
                        BasePrice = basePrice,
                        Rooms = roomList.OrderBy(x => x).ToList()
                    };

                    movies[id] = m;
                    revenue[id] = 0;

                    foreach (int r in m.Rooms)
                    {
                        sold[(id, r)] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    }

                    id++;
                }
            }
        }


        private void btnStart_Click(object sender, EventArgs e)
        {
            if (running)
            {
                StopServer();
                return;
            }

            if (movies.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn file chứa dữ liệu phim.");
                return;
            }

            try
            {
                listener = new TcpListener(IPAddress.Any, 9000);
                listener.Start();
                running = true;
                acceptThread = new Thread(AcceptLoop) { IsBackground = true };
                acceptThread.Start();

                AppendLog("Server đang lắng nghe tại port 9000...");
                SetStatus("Lắng nghe (9000)");
                StartButton.Text = "Tắt Server";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể khởi động server: " + ex.Message);
            }
            
        }

        private void StopServer()
        {
            try
            {
                running = false;
                listener?.Stop();
                acceptThread?.Join(300);
                lock (clientStreams)
                {
                    foreach (var writer in clientStreams)
                    {
                        try
                        {
                            writer.BaseStream.Close();
                            writer.Close();
                        }
                        catch { }
                    }
                    clientStreams.Clear(); 
                }
                AppendLog("Server đã dừng.");
                SetStatus("Tắt server");
                StartButton.Text = "Bật server (9000)";
            }
            catch { }
        }

        private void AcceptLoop()
        {
            while (running)
            {
                try
                {
                    var client = listener.AcceptTcpClient();
                    ThreadPool.QueueUserWorkItem(HandleClient, client);
                }
                catch (SocketException)
                {
                    if (!running) break;
                }
                catch (Exception ex)
                {
                    AppendLog("Có lỗi xảy ra: " + ex.Message);
                }
            }
        }

        private void HandleClient(object state)
        {
            var tcp = (TcpClient)state;
            var remote = tcp.Client.RemoteEndPoint?.ToString() ?? "client";
            AppendLog($"Kết nối mới: {remote}");

            StreamWriter sw = null; 

            try
            {
                using (var ns = tcp.GetStream())
                using (var sr = new StreamReader(ns, new UTF8Encoding(false), true))
                {
                    using (var swLocal = new StreamWriter(ns, new UTF8Encoding(false)) { AutoFlush = true })
                    {
                        sw = swLocal; 

                        swLocal.WriteLine("WELCOME");

                        lock (clientStreams)
                        {
                            clientStreams.Add(swLocal);
                        }

                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            if (string.IsNullOrWhiteSpace(line)) continue;

                            var parts = line.Split('|');
                            var cmd = parts[0].Trim().Trim('\uFEFF').ToUpperInvariant();

                            if (cmd == "GET_MOVIES")
                            {
                                AppendLog(">> GET_MOVIES");
                                HandleGetMovies(swLocal);
                                AppendLog("<< MOVIES sent");
                            }
                            else if (cmd == "GET_STATE" &&
                                     parts.Length >= 3 &&
                                     int.TryParse(parts[1], out int movieId) &&
                                     int.TryParse(parts[2], out int room))
                            {
                                HandleGetState(swLocal, movieId, room);
                            }
                            else if (cmd == "BOOK" &&
                                     parts.Length >= 5 &&
                                     int.TryParse(parts[1], out int mid) &&
                                     int.TryParse(parts[2], out int r))
                            {
                                HandleBook(swLocal, mid, r, parts[3].Trim(), parts[4].Trim());
                            }
                            else
                            {
                                swLocal.WriteLine("ERR|UnknownCommand");
                            }
                        }
                    }
                }
            }
            catch (IOException)
            {
            }
            catch (Exception ex)
            {
                AppendLog($"Lỗi client {remote}: {ex.Message}");
            }
            finally
            {
                if (sw != null)
                {
                    lock (clientStreams)
                    {
                        clientStreams.Remove(sw);
                    }
                }
                AppendLog($"Ngắt kết nối: {remote}");
            }
        }

        private void Broadcast(string message)
        {
            lock (clientStreams)
            {
                foreach (var w in clientStreams.ToList())
                {
                    try { w.WriteLine(message); }
                    catch {  }
                }
            }
        }

        private void HandleGetMovies(StreamWriter sw)
        {
            lock (_lock)
            {
                sw.WriteLine($"MOVIES|{movies.Count}");
                foreach (var m in movies.Values.OrderBy(m => m.Id))
                {
                    sw.WriteLine($"MOVIE|{m.Id}|{m.Name}|{m.BasePrice}|{string.Join(",", m.Rooms)}");
                }
            }
        }

        private void HandleGetState(StreamWriter sw, int movieId, int room)
        {
            lock (_lock)
            {
                if (!movies.ContainsKey(movieId) || !movies[movieId].Rooms.Contains(room))
                {
                    sw.WriteLine($"STATE|{movieId}|{room}|");
                    return;
                }

                var key = (movieId, room);
                if (!sold.TryGetValue(key, out var hs) || hs.Count == 0)
                {
                    sw.WriteLine($"STATE|{movieId}|{room}|");
                    return;
                }

                sw.WriteLine($"STATE|{movieId}|{room}|{string.Join(",", hs.OrderBy(s => s))}");
            }
        }

        private void HandleBook(StreamWriter sw, int movieId, int room, string seatsCsv, string customer)
        {
            if (string.IsNullOrWhiteSpace(customer))
            {
                sw.WriteLine("BOOK_FAIL|Thiếu tên khách hàng");
                return;
            }

            customer = customer.Trim();

            var seats = seatsCsv.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim().ToUpperInvariant())
                .Distinct()
                .ToList();

            if (seats.Count == 0)
            {
                sw.WriteLine("BOOK_FAIL|Không có ghế");
                return;
            }

            lock (_lock)
            {
                if (!movies.ContainsKey(movieId) || !movies[movieId].Rooms.Contains(room))
                {
                    sw.WriteLine("BOOK_FAIL|Phim hoặc phòng không hợp lệ");
                    return;
                }

                var key = (movieId, room);
                if (!sold.TryGetValue(key, out var hs))
                {
                    hs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    sold[key] = hs;
                }

                foreach (var s in seats)
                {
                    if (!IsValidSeatName(s))
                    {
                        sw.WriteLine($"BOOK_FAIL|Ghế không hợp lệ: {s}");
                        return;
                    }
                    if (hs.Contains(s))
                    {
                        sw.WriteLine($"BOOK_FAIL|Ghế đã bán: {s}");
                        return;
                    }
                }

                if (!customerTickets.TryGetValue(customer, out int oldTotal))
                    oldTotal = 0;

                if (!customerRooms.TryGetValue(customer, out var roomSet))
                {
                    roomSet = new HashSet<(int, int)>();
                    customerRooms[customer] = roomSet;
                }

                bool hasThisRoom = roomSet.Contains((movieId, room));
                int newTotal = oldTotal + seats.Count;
                int newRoomCount = roomSet.Count + (hasThisRoom ? 0 : 1);

               
                if (newRoomCount >= 2 && newTotal > 2)
                {
                    sw.WriteLine("BOOK_FAIL|Không thể chọn hơn 2 vé ở 2 phòng chiếu khác nhau.");
                    return;
                }

                int total = seats.Sum(s => CalculateSeatPrice(movies[movieId].BasePrice, s));
                foreach (var s in seats)
                    hs.Add(s);

                revenue[movieId] = (revenue.TryGetValue(movieId, out int oldRev) ? oldRev : 0) + total;

                roomSet.Add((movieId, room));
                customerTickets[customer] = newTotal;

                sw.WriteLine($"BOOK_OK|{total}");
                AppendLog($"BOOK_OK {customer} | {movies[movieId].Name} - P{room} | {string.Join(",", seats)} | {total:N0}");

                string state = string.Join(",", hs.OrderBy(x => x));
                Broadcast($"UPDATE_STATE|{movieId}|{room}|{state}");
            }
        }


        private static bool IsValidSeatName(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;
            s = s.ToUpperInvariant();
            if (s[0] != 'A' && s[0] != 'B' && s[0] != 'C') return false;
            if (!int.TryParse(s.Substring(1), out int col)) return false;
            return col >= 1 && col <= 5;
        }

        private static int CalculateSeatPrice(int basePrice, string seat)
        {
            seat = seat.ToUpperInvariant();
            double factor;

            if (seat == "A1" || seat == "A5" || seat == "C1" || seat == "C5")
                factor = 0.25;
            else if (seat == "B2" || seat == "B3" || seat == "B4")
                factor = 2.0;
            else
                factor = 1.0;

            return (int)(basePrice * factor);
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            if (movies.Count == 0)
            {
                MessageBox.Show("Chưa có dữ liệu phim.");
                return;
            }

            using (var sfd = new SaveFileDialog
            {
                Title = "Lưu output5.txt",
                Filter = "Text files (*.txt)|*.txt",
                AddExtension = true,
                DefaultExt = "txt",
                FileName = "output5.txt",
                InitialDirectory = Application.StartupPath
            })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                    ThreadPool.QueueUserWorkItem(_ => ExportStats(sfd.FileName));
            }
        }

        private void ExportStats(string path)
        {
            try
            {
                SetStatus("Đang xuất thống kê...");
                var lines = new List<string> { "TEN_PHIM|VE_BAN|VE_TON|TI_LE_BAN(%)|DOANH_THU|XEP_HANG_DOANH_THU" };

                Dictionary<int, int> soldCount = new Dictionary<int, int>();
                Dictionary<int, int> capacity = new Dictionary<int, int>();

                lock (_lock)
                {
                    foreach (var m in movies.Values)
                    {
                        int soldSeats = 0;
                        foreach (int r in m.Rooms)
                            if (sold.TryGetValue((m.Id, r), out var hs))
                                soldSeats += hs.Count;

                        soldCount[m.Id] = soldSeats;
                        capacity[m.Id] = m.Rooms.Count * 15;
                    }
                }

                var rank = revenue
                    .OrderByDescending(kv => kv.Value)
                    .Select((kv, i) => (MovieId: kv.Key, Rank: i + 1))
                    .ToDictionary(x => x.MovieId, x => x.Rank);

                int i = 0;
                int total = Math.Max(1, movies.Count);

                foreach (var m in movies.Values.OrderBy(m => m.Id))
                {
                    int soldSeats = soldCount[m.Id];
                    int cap = capacity[m.Id];
                    int rev = revenue.TryGetValue(m.Id, out int rv) ? rv : 0;
                    int remain = Math.Max(0, cap - soldSeats);
                    double ratio = cap > 0 ? soldSeats * 100.0 / cap : 0.0;
                    int rk = rank.TryGetValue(m.Id, out int rnk) ? rnk : 0;

                    lines.Add($"{m.Name}|{soldSeats}|{remain}|{ratio:0.00}|{rev}|{rk}");

                    i++;
                    SetProgress(i, total);
                }

                File.WriteAllLines(path, lines, Encoding.UTF8);
                AppendLog($"Đã xuất thống kê -> {path}");
                SetStatus("Xuất xong");
            }
            catch (Exception ex)
            {
                AppendLog("Xuất thống kê lỗi: " + ex.Message);
                SetStatus("Lỗi xuất thống kê");
            }
        }

        private void Server_FormClosed(object sender, FormClosedEventArgs e) => StopServer();
    }
}
