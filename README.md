# VietKey

Bộ gõ tiếng Việt cho Windows viết bằng **C# / .NET 8 / WinForms**. Đây là bản viết lại từ đầu theo cách UniKey hoạt động (không dùng code của UniKey):

- hook bàn phím cấp thấp `WH_KEYBOARD_LL` cho toàn hệ thống;
- engine Telex/VNI tự viết, tách riêng để test được;
- thay chữ bằng `SendInput` (Backspace + ký tự Unicode `KEYEVENTF_UNICODE`);
- icon ở khay hệ thống, lưu cấu hình vào `HKCU\Software\VietKey`.

## Build & chạy

```powershell
dotnet test                                  # chạy unit test của engine
dotnet run --project src/VietKey             # chạy bộ gõ
dotnet publish src/VietKey -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```

Có thể mở `VietKey.sln` bằng Visual Studio 2022 để build.

## Sử dụng

- **Bấm chuột trái** vào icon **V** (đỏ, tiếng Việt) hoặc **E** (xanh, tiếng Anh) để chuyển chế độ; hoặc dùng **Ctrl+Shift** (mặc định) / **Alt+Z**. Bấm đúp mở Bảng điều khiển.
- **Menu chuột phải** (bố cục UniKey): Hướng dẫn · Công cụ...[CS+F6] · Chuyển mã nhanh [CS+F9] · Bật kiểm tra chính tả · Bật tính năng gõ tắt · Soạn bảng gõ tắt... · Kiểu gõ ▸ · Unicode dựng sẵn / TCVN3 (ABC) / VNI Windows / Bảng mã khác ▸ · **Bảng điều khiển...[CS+F5]** · Kết thúc.
- **Bảng điều khiển** (Ctrl+Shift+F5): phần *Điều khiển* (bảng mã, kiểu gõ, phím chuyển) và nút *Mở rộng* để hiện:
  - *Tùy chọn khác*: gõ tự do, đặt dấu oà/uý, luôn dùng clipboard cho Unicode, kiểm tra chính tả, tự động khôi phục phím với từ sai, thông báo phản hồi, chuông khi chuyển ngôn ngữ;
  - *Tùy chọn gõ tắt*: bật gõ tắt, gõ tắt cả khi tắt tiếng Việt, Bảng gõ tắt...;
  - *Hệ thống*: hiện hộp thoại khi khởi động, khởi động cùng Windows, giao diện tiếng Việt/Anh.
  Mọi thay đổi có hiệu lực ngay; *Mặc định* đưa về cấu hình giống UniKey.

| Tuỳ chọn | Ý nghĩa |
|---|---|
| Bảng mã | Bảng mã của chữ gõ ra: Unicode dựng sẵn, TCVN3, VNI Windows, Unicode tổ hợp |
| Gõ tự do | Bật: gõ dấu ở cuối từ (`toans` → toán). Tắt: phải gõ dấu ngay sau nguyên âm (`toasn`) |
| Luôn dùng clipboard cho Unicode | Gửi chữ bằng Ctrl+V thay vì ký tự Unicode (cho ứng dụng cũ); clipboard cũ được trả lại sau đó |
| Kiểm tra chính tả | Chỉ đặt dấu khi từ là âm tiết tiếng Việt hợp lệ; cần bật để *khôi phục phím với từ sai* hoạt động |
| Gõ tắt | Gõ từ tắt + Space/Enter/Tab. Bảng lưu ở `%AppData%\VietKey\macros.txt` (UTF-8, `từ<TAB>nội dung`) |

| Telex | | VNI | |
|---|---|---|---|
| s f r x j | sắc huyền hỏi ngã nặng | 1 2 3 4 5 | sắc huyền hỏi ngã nặng |
| z | xoá dấu | 0 | xoá dấu |
| aa ee oo | â ê ô | 6 | â ê ô |
| aw ow uw, uow | ă ơ ư, ươ | 8 / 7 | ă / ơ ư ươ |
| dd | đ | 9 | đ |
| w (đứng riêng) | ư | | |

## Chuyển mã (giống UniKey Toolkit)

Chuột phải icon → **Công cụ...**: chọn bảng mã nguồn/đích rồi

- **Chuyển mã clipboard**: copy văn bản cũ (ví dụ từ file Word dùng font .VnTime) → bấm nút → dán (Ctrl+V);
- **Chuyển ô văn bản**: dán vào ô nguồn để xem kết quả ngay;
- **Chuyển mã file...**: file văn bản thuần (.txt, .csv, .html). TCVN3/VNI đọc/ghi theo byte, Unicode theo UTF-8.

**Ctrl+Shift+F6** mở Công cụ; **Ctrl+Shift+F9** (hoặc menu *Chuyển mã nhanh*) chuyển clipboard theo cặp bảng mã đã chọn lần trước, không cần mở cửa sổ.

| Bảng mã | Nguồn | Đích | Ví dụ "Việt Nam" |
|---|---|---|---|
| Unicode dựng sẵn | ✓ | ✓ | `Việt Nam` |
| Unicode tổ hợp | ✓ | ✓ | `Viê` + U+0323 + `t Nam` |
| TCVN3 (ABC) | ✓ | ✓ | `ViÖt Nam` |
| VNI Windows | ✓ | ✓ | `Vieät Nam` |
| Không dấu | | ✓ | `Viet Nam` |

TCVN3 không có mã cho chữ hoa mang dấu thanh (Ấ, Ệ…), nên khi chuyển sang TCVN3 các chữ này dùng mã chữ thường (font .VnTimeH vẫn hiển thị thành chữ hoa) và không chuyển ngược về chữ hoa được.

## Ghi chú khi gõ

Gõ lại phím vừa dùng để huỷ (`ass` → `as`, `ww` → `w`). Khi từ không phải âm tiết tiếng Việt hợp lệ, bộ gõ trả lại đúng các phím đã gõ (`Windows`, `good`).

## Cấu trúc

```
src/VietKey.Core/          engine thuần C#, không phụ thuộc Windows
  Engine/VietChars.cs      bảng nguyên âm + thanh điệu
  Engine/VietEngine.cs     ghép chữ, đặt dấu, kiểm tra cấu trúc âm tiết
  TypingSession.cs         so sánh trước/sau → số Backspace + chuỗi cần gửi
  Conversion/VietConverter.cs  bảng mã Unicode / tổ hợp / TCVN3 / VNI / không dấu
  Macros/MacroTable.cs     bảng gõ tắt
src/VietKey/               ứng dụng WinForms
  Input/KeyboardHook.cs    hook bàn phím/chuột, phím tắt bật/tắt
  Input/InputSender.cs     SendInput
  App/TrayApp.cs           icon khay hệ thống + menu
  App/ControlPanelForm.cs  bảng điều khiển (thu nhỏ / mở rộng như UniKey)
  App/MacroEditorForm.cs   soạn bảng gõ tắt
  App/Lang.cs              giao diện tiếng Việt / tiếng Anh
  App/ConverterForm.cs     cửa sổ chuyển mã
  App/ClipboardConverter.cs  chuyển mã clipboard và file
  App/AppSettings.cs       cấu hình trong Registry, khởi động cùng Windows
tests/VietKey.Core.Tests/  xUnit
```

## Giới hạn hiện tại

- Kiểu gõ chỉ có Telex và VNI (chưa có VIQR / tự định nghĩa: nút "···" đang tắt).
- Chuyển mã clipboard chỉ xử lý văn bản thuần, không xử lý RTF/HTML (định dạng, tên font không đổi).
- Chưa có bảng loại trừ ứng dụng (tự tắt tiếng Việt cho từng chương trình).
- Chưa kiểm tra chính tả đầy đủ: chỉ kiểm phụ âm đầu/cuối và số nguyên âm, nên từ tiếng Anh như `test` vẫn thành `tét`.
- Không gõ được vào cửa sổ chạy quyền Administrator (giới hạn UIPI của Windows), trừ khi chạy VietKey bằng quyền admin.
- Một số ô có tự động gợi ý (thanh địa chỉ trình duyệt, Excel) có thể xử lý Backspace không như ý; UniKey có cách xử lý riêng cho các trường hợp này.
