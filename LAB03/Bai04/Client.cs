using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Bai04
{
    public partial class Client : Form
    {
        private const string ServerIp = "10.229.152.89";
        private const int ServerPort = 9000;
        private bool isConnected = false;

        private TcpClient tcpClient;
        private StreamReader sreader;
        private StreamWriter swriter;

        private Movie currentMovie;
        private List<Movie> movies = new List<Movie>();
        private int currentBasePrice = 0;
        private int currentRoom = 0;

        private List<Button> seatButtons = new List<Button>();
        private HashSet<string> selectedSeats = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private class CustomerInfo
        {
            public int TotalTickets = 0; 
            public HashSet<(int movieId, int room)> Rooms = new HashSet<(int, int)>();
        }

        private readonly Dictionary<string, CustomerInfo> customerInfos =
            new Dictionary<string, CustomerInfo>(StringComparer.OrdinalIgnoreCase);

        private readonly object _pendingLock = new object();
        private TaskCompletionSource<List<Movie>> _pendingMovies;
        private TaskCompletionSource<StateResult> _pendingState;
        private TaskCompletionSource<string> _pendingBook;

        private class StateResult
        {
            public int MovieId;
            public int Room;
            public HashSet<string> SoldSeats;
        }

        public Client()
        {
            InitializeComponent();

            seatButtons.Add(A1);
            seatButtons.Add(A2);
            seatButtons.Add(A3);
            seatButtons.Add(A4);
            seatButtons.Add(A5);

            seatButtons.Add(B1);
            seatButtons.Add(B2);
            seatButtons.Add(B3);
            seatButtons.Add(B4);
            seatButtons.Add(B5);

            seatButtons.Add(C1);
            seatButtons.Add(C2);
            seatButtons.Add(C3);
            seatButtons.Add(C4);
            seatButtons.Add(C5);

            foreach (var btn in seatButtons)
            {
                btn.Tag = btn.BackColor;
            }

            MovieComboBox.Enabled = false;
            RoomComboBox.Enabled = false;
            BookButton.Enabled = false;
        }

        public class Movie
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int BasePrice { get; set; }
            public List<int> Rooms { get; set; }

            public Movie()
            {
                Rooms = new List<int>();
            }
            public override string ToString()
            {
                return Name;
            }
        }
       

        private async void MovieComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            currentMovie = MovieComboBox.SelectedItem as Movie;

            RoomComboBox.Items.Clear();
            currentRoom = 0;
            selectedSeats.Clear();
            Total.Text = "0";
            foreach (var btn in seatButtons)
            {
                btn.Enabled = false;
                btn.BackColor = (Color)btn.Tag;
            }

            if (currentMovie != null)
            {
                currentBasePrice = currentMovie.BasePrice;

                foreach (int r in currentMovie.Rooms)
                {
                    RoomComboBox.Items.Add(r);
                }

                if (RoomComboBox.Items.Count > 0)
                {
                    if (RoomComboBox.SelectedIndex < 0)
                        RoomComboBox.SelectedIndex = 0;
                    await RefreshSeatsFromServerAsync();
                }
            }
        }

        private async void RoomComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (RoomComboBox.SelectedItem == null || currentMovie == null)
            {
                foreach (var btn in seatButtons)
                {
                    btn.Enabled = false;
                    btn.BackColor = (Color)btn.Tag;
                }
                return;
            }

            currentRoom = (int)RoomComboBox.SelectedItem;

            await RefreshSeatsFromServerAsync();
        }

        private void SeatButton_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            if (btn == null) return;
            if (!btn.Enabled) return;

            if (currentMovie == null || currentRoom == 0)
            {
                MessageBox.Show("Vui lòng kết nối server và chọn phim,phòng.");
                return;
            }

            string seatName = btn.Text.Trim().ToUpper();

            if (selectedSeats.Contains(seatName))
            {
                selectedSeats.Remove(seatName);
                btn.BackColor = (Color)btn.Tag;
            }
            else
            {
               
                selectedSeats.Add(seatName);
                btn.BackColor = Color.LightGreen;
            }

            int total = 0;
            foreach (string s in selectedSeats)
            {
                total += CalculateSeatPrice(s);
            }
            Total.Text = total.ToString();
        }

        private async void BookButton_Click(object sender, EventArgs e)
        {
           
            if (selectedSeats.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn ít nhất một ghế.");
                return;
            }

            string customer = CustomerNameBox.Text.Trim();
            if (string.IsNullOrEmpty(customer))
            {
                MessageBox.Show("Vui lòng nhập tên khách hàng.");
                return;
            }

            try
            {
                int movieId = currentMovie.Id;
                var seatsJustBooked = selectedSeats.ToList();
                string seatList = string.Join(",", selectedSeats);
                int newSeatsCount = seatsJustBooked.Count;
                if (!customerInfos.TryGetValue(customer, out CustomerInfo info))
                {
                    info = new CustomerInfo();
                    customerInfos[customer] = info;
                }

                int oldTotal = info.TotalTickets;
                int oldRoomCount = info.Rooms.Count;
                bool hasThisRoom = info.Rooms.Contains((movieId, currentRoom));

                int newTotal = oldTotal + newSeatsCount;
                int newRoomCount = oldRoomCount + (hasThisRoom ? 0 : 1);
                bool aboutToLockToOneRoom = false;

                if (oldTotal == 0 && newRoomCount == 1 && newTotal >= 2)
                    aboutToLockToOneRoom = true;

                if (oldTotal == 1 && oldRoomCount == 1 && hasThisRoom && newRoomCount == 1)
                    aboutToLockToOneRoom = true;

                if (aboutToLockToOneRoom)
                {
                    var ans = MessageBox.Show(
                        "Lưu ý: Nếu bạn mua từ 2 vé trở lên nhưng CHỈ trong MỘT phòng chiếu, thì sau này bạn sẽ KHÔNG thể mua thêm vé ở phòng chiếu khác.\n\n" +
                        "Bạn có chắc chắn vẫn muốn tiếp tục không?",
                        "Xác nhận mua nhiều vé trong một phòng",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (ans == DialogResult.No)
                    {
                        return;
                    }
                }

                if (newRoomCount >= 2 && newTotal > 2)
                {
                    MessageBox.Show(
                        "Không thể chọn hơn 2 vé ở 2 phòng chiếu khác nhau.",
                        "Thông báo",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                string resp = await RequestBookAsync(movieId, currentRoom, seatList, customer);
                if (resp == null)
                {
                    MessageBox.Show("Server đã ngắt kết nối.");
                    return;
                }

                string[] p = resp.Split('|');
                if (p[0] == "BOOK_OK" && p.Length >= 2)
                {
                    int totalFromServer = int.Parse(p[1]);

                    info.TotalTickets += seatsJustBooked.Count;
                    info.Rooms.Add((movieId, currentRoom));

                    MessageBox.Show(
                        "Đặt vé thành công!\n" +
                        "Khách hàng: " + customer + "\n" +
                        "Phim: " + currentMovie.Name + "\n" +
                        "Phòng số: " + currentRoom + "\n" +
                        "Ghế: " + seatList + "\n" +
                        "Tổng tiền: " + totalFromServer);

                    foreach (var seat in seatsJustBooked)
                    {
                        var btnSeat = seatButtons.FirstOrDefault(b =>
                            string.Equals(b.Text.Trim(), seat, StringComparison.OrdinalIgnoreCase));
                        if (btnSeat != null)
                        {
                            btnSeat.Enabled = false;
                            btnSeat.BackColor = Color.LightGray;
                        }
                    }

                    selectedSeats.Clear();
                    Total.Text = totalFromServer.ToString();
                    await RefreshSeatsFromServerAsync();
                }
                else if (p[0] == "BOOK_FAIL" && p.Length >= 2)
                {
                    MessageBox.Show("Đặt vé thất bại: " + p[1]);
                    selectedSeats.Clear();
                    Total.Text = "0";

                    await RefreshSeatsFromServerAsync();
                }
                else
                {
                    MessageBox.Show("Phản hồi không hợp lệ từ server: " + resp);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi đặt vé(kiểm tra lại kết nối)");
            }
        }
        
        private int CalculateSeatPrice(string seat)
        {
            seat = seat.ToUpper();
            double factor;

            if (seat == "A1" || seat == "A5" || seat == "C1" || seat == "C5")
            {
                factor = 0.25;
            }
            else if (seat == "B2" || seat == "B3" || seat == "B4")
            {
                factor = 2.0;
            }
            else
            {
                factor = 1.0;
            }

            int price = (int)(currentBasePrice * factor);
            return price;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);

            try
            {
                tcpClient?.Close();
            }
            catch { }
        }

        private async void ConnectButton_Click(object sender, EventArgs e)
        {
            if (tcpClient != null && tcpClient.Connected)
            {
                MessageBox.Show("Đã kết nối server rồi.");
                return;
            }

            try
            {
                tcpClient = new TcpClient();
                tcpClient.Connect(ServerIp, ServerPort);

                NetworkStream ns = tcpClient.GetStream();


                sreader = new StreamReader(
                    ns,
                    new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                    detectEncodingFromByteOrderMarks: true
                );
                swriter = new StreamWriter(
                    ns,
                    new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
                )
                { AutoFlush = true };

                await Task.Run(() =>
                {
                    try { sreader.ReadLine(); }
                    catch {  }
                });
                isConnected = true;

                MovieComboBox.Enabled = true;
                RoomComboBox.Enabled = true;
                BookButton.Enabled = true;

                Task.Run(() => ListenFromServer());

                try
                {
                    await LoadMoviesFromServerAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi khi tải phim sau khi kết nối:" );
                }

                MessageBox.Show("Kết nối server thành công!");
            }
            catch (Exception ex)
            {
                isConnected = false;
                MovieComboBox.Enabled = false;
                RoomComboBox.Enabled = false;
                BookButton.Enabled = false;

                MessageBox.Show("Không kết nối được server:" );

                if (tcpClient != null)
                {
                    try { tcpClient.Close(); } catch { }
                    tcpClient = null;
                    sreader = null;
                    swriter = null;
                }
            }
        }



        private Task<List<Movie>> RequestMoviesAsync()
        {
            TaskCompletionSource<List<Movie>> tcs;
            lock (_pendingLock)
            {
                tcs = new TaskCompletionSource<List<Movie>>();
                _pendingMovies = tcs;
            }
            swriter.WriteLine("GET_MOVIES");
            return tcs.Task;
        }

        private Task<StateResult> RequestStateAsync(int movieId, int room)
        {
            TaskCompletionSource<StateResult> tcs;
            lock (_pendingLock)
            {
                tcs = new TaskCompletionSource<StateResult>();
                _pendingState = tcs;
            }
            swriter.WriteLine($"GET_STATE|{movieId}|{room}");
            return tcs.Task;
        }

        private Task<string> RequestBookAsync(int movieId, int room, string seatsCsv, string customer)
        {
            TaskCompletionSource<string> tcs;
            lock (_pendingLock)
            {
                tcs = new TaskCompletionSource<string>();
                _pendingBook = tcs;
            }
            swriter.WriteLine($"BOOK|{movieId}|{room}|{seatsCsv}|{customer}");
            return tcs.Task;
        }

        private void ListenFromServer()
        {
            try
            {
                while (tcpClient != null && tcpClient.Connected)
                {
                    string line = sreader.ReadLine();
                    if (line == null) break;

                    var parts = line.Split('|');
                    if (parts.Length == 0) continue;

                    string cmd = parts[0].Trim().ToUpperInvariant();

                    switch (cmd)
                    {
                        case "MOVIES":
                            HandleMoviesResponse(parts);
                            break;

                        case "STATE":
                            HandleStateResponse(parts);
                            break;

                        case "BOOK_OK":
                        case "BOOK_FAIL":
                            HandleBookResponse(line);
                            break;

                        case "UPDATE_STATE":
                            HandleUpdateStateBroadcast(parts);
                            break;

                        case "ERR":
                            break;

                        default:
                            break;
                    }
                }
            }
            catch
            {
            }
            finally
            {
                this.Invoke(new Action(() =>
                {
                    MessageBox.Show("Mất kết nối với Server!");

                    isConnected = false;

                    if (tcpClient != null)
                    {
                        try { tcpClient.Close(); } catch { }
                        tcpClient = null; 
                    }
                    sreader = null;
                    swriter = null;

                    MovieComboBox.Enabled = false;
                    RoomComboBox.Enabled = false;
                    BookButton.Enabled = false;

                    MovieComboBox.Items.Clear();
                    RoomComboBox.Items.Clear();
                    foreach (var btn in seatButtons)
                    {
                        btn.Enabled = false;
                        btn.BackColor = (Color)btn.Tag;
                    }
                }));
            }
        }

        private void HandleMoviesResponse(string[] partsHeader)
        {
            if (partsHeader.Length < 2 || !int.TryParse(partsHeader[1], out int count))
            {
                TaskCompletionSource<List<Movie>> tcsErr;
                lock (_pendingLock)
                {
                    tcsErr = _pendingMovies;
                    _pendingMovies = null;
                }
                tcsErr?.TrySetResult(new List<Movie>());
                return;
            }

            var list = new List<Movie>();

            for (int i = 0; i < count; i++)
            {
                string lineMovie = sreader.ReadLine();
                if (lineMovie == null) break;

                var p = lineMovie.Split('|');
                if (p.Length < 5 || !p[0].Equals("MOVIE", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!int.TryParse(p[1], out int id)) continue;
                if (!int.TryParse(p[3], out int bp)) continue;

                var m = new Movie
                {
                    Id = id,
                    Name = p[2],
                    BasePrice = bp,
                    Rooms = p[4].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(tok => int.TryParse(tok, out var rr) ? rr : 0)
                                .Where(rr => rr > 0).ToList()
                };
                list.Add(m);
            }

            TaskCompletionSource<List<Movie>> tcs;
            lock (_pendingLock)
            {
                tcs = _pendingMovies;
                _pendingMovies = null;
            }
            tcs?.TrySetResult(list);
        }

        private void HandleStateResponse(string[] parts)
        {
            if (parts.Length < 3) return;

            int movieId, room;
            if (!int.TryParse(parts[1], out movieId)) return;
            if (!int.TryParse(parts[2], out room)) return;

            var soldSeats = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (parts.Length >= 4 && !string.IsNullOrEmpty(parts[3]))
            {
                var seats = parts[3].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var s in seats)
                    soldSeats.Add(s.Trim().ToUpper());
            }

            var result = new StateResult
            {
                MovieId = movieId,
                Room = room,
                SoldSeats = soldSeats
            };

            TaskCompletionSource<StateResult> tcs;
            lock (_pendingLock)
            {
                tcs = _pendingState;
                _pendingState = null;
            }
            tcs?.TrySetResult(result);
        }

        private void HandleBookResponse(string fullLine)
        {
            TaskCompletionSource<string> tcs;
            lock (_pendingLock)
            {
                tcs = _pendingBook;
                _pendingBook = null;
            }
            tcs?.TrySetResult(fullLine);
        }

        private void HandleUpdateStateBroadcast(string[] parts)
        {
            if (parts.Length < 3) return;
            if (!int.TryParse(parts[1], out int movieId)) return;
            if (!int.TryParse(parts[2], out int room)) return;
            string seatsCsv = parts.Length >= 4 ? parts[3] : "";

            if (currentMovie != null && currentMovie.Id == movieId && currentRoom == room)
            {
                this.Invoke(new Action(() =>
                {
                    UpdateSeatsFromState(seatsCsv);
                }));
            }
        }
        private async Task LoadMoviesFromServerAsync()
        {
            int? oldMovieId = currentMovie?.Id;
            int oldRoom = currentRoom;

            var list = await RequestMoviesAsync();

            movies = list;

            MovieComboBox.Items.Clear();
            RoomComboBox.Items.Clear();
            currentMovie = null;
            currentRoom = 0;

            foreach (var m in movies)
                MovieComboBox.Items.Add(m);

            if (movies.Count == 0)
            {
                MessageBox.Show("Không có phim nào từ server.");
                return;
            }

            int movieIndex = 0;
            if (oldMovieId.HasValue)
            {
                for (int i = 0; i < movies.Count; i++)
                {
                    if (movies[i].Id == oldMovieId.Value)
                    {
                        movieIndex = i;
                        break;
                    }
                }
            }

            MovieComboBox.SelectedIndex = movieIndex;
            currentMovie = (Movie)MovieComboBox.SelectedItem;
            currentBasePrice = currentMovie.BasePrice;

            RoomComboBox.Items.Clear();
            foreach (int r in currentMovie.Rooms)
                RoomComboBox.Items.Add(r);

            if (RoomComboBox.Items.Count == 0)
            {
                currentRoom = 0;
                return;
            }

            int roomIndex = 0;
            if (oldRoom != 0 && currentMovie.Rooms.Contains(oldRoom))
                roomIndex = currentMovie.Rooms.IndexOf(oldRoom);

            RoomComboBox.SelectedIndex = roomIndex;
            currentRoom = (int)RoomComboBox.SelectedItem;

            await RefreshSeatsFromServerAsync();
        }

        private async Task RefreshSeatsFromServerAsync()
        {
            if (currentMovie == null || currentRoom == 0 ||
                swriter == null || sreader == null)
                return;

            selectedSeats.Clear();
            Total.Text = "0";

            foreach (var btn in seatButtons)
            {
                btn.Enabled = true;
                btn.BackColor = (Color)btn.Tag;
            }

            try
            {
                var state = await RequestStateAsync(currentMovie.Id, currentRoom);

                if (currentMovie == null ||
                    state.MovieId != currentMovie.Id ||
                    state.Room != currentRoom)
                    return;

                foreach (var btn in seatButtons)
                {
                    string seatName = btn.Text.Trim().ToUpper();
                    if (state.SoldSeats.Contains(seatName))
                    {
                        btn.Enabled = false;
                        btn.BackColor = Color.LightGray;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi tải phòng: " + ex.Message);
            }
        }

        private void UpdateSeatsFromState(string seatsCsv)
        {
            var soldSeats = new HashSet<string>(
                seatsCsv.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim().ToUpper()),
                StringComparer.OrdinalIgnoreCase);

            foreach (var btn in seatButtons)
            {
                string seat = btn.Text.Trim().ToUpper();
                if (soldSeats.Contains(seat))
                {
                    btn.Enabled = false;
                    btn.BackColor = Color.LightGray;
                }
                else
                {
                    btn.Enabled = true;
                    btn.BackColor = (Color)btn.Tag;
                }
            }

            selectedSeats.Clear();
            Total.Text = "0";
        }
    }
}
