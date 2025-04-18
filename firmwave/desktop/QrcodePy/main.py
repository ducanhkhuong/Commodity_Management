import qrcode # type: ignore
import os

output_dir = "output"
if not os.path.exists(output_dir):
    os.makedirs(output_dir)

input_string = input("Nhập nội dung chuỗi để tạo mã QR: ")


qr = qrcode.QRCode(
    version=1,
    error_correction=qrcode.constants.ERROR_CORRECT_L,
    box_size=10,
    border=4,
)


qr.add_data(input_string)
qr.make(fit=True)


qr_image = qr.make_image(fill_color="black", back_color="white")

file_name = input_string.replace(" ", "_")[:50] + "_qr.png"
file_path = os.path.join(output_dir, file_name)

qr_image.save(file_path)

print(f"Mã QR đã được lưu tại: {file_path}")