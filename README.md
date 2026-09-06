# Amazon Seller Central Image Packager

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Avalonia](https://img.shields.io/badge/Avalonia-12.1.2-8B44AC?style=flat-square)](https://avaloniaui.net/)
[![License](https://img.shields.io/github/license/spreedated/AmazonSellerCentralImagePackager?style=flat-square)](LICENSE.txt)
[![Last Commit](https://img.shields.io/github/last-commit/spreedated/AmazonSellerCentralImagePackager?style=flat-square)](https://github.com/spreedated/AmazonSellerCentralImagePackager/commits/master)
	
[!["Buy Me A Coffee"](https://www.buymeacoffee.com/assets/img/custom_images/orange_img.png)](https://buymeacoffee.com/spreed)

Amazon Seller Central Image Packager is a desktop utility built with Avalonia and C# that streamlines the process of creating image upload packages for Amazon listings. It is designed for sellers or integrators who regularly manage product images across multiple ASINs.

## ✨ Features

- Input multiple ASINs at once
- Choose a single image to apply to all ASINs
- Automatically creates ZIP archives per ASIN
- Output files are Amazon-ready for bulk image upload
- Cross-platform (Windows, Linux, macOS) via Avalonia

## 📦 Use Case

Let's say you're launching a set of identical products under different ASINs or updating a shared product image. Instead of manually creating and naming folders or archives, Amazon Seller Central Image Packager does it all in seconds.

## 🚀 How It Works

1. Start the application.
2. Paste or type in the list of ASINs (one per line, it recognizes correctly formatted strings).
3. Select the product image file (JPEG/PNG).
4. Choose which Amazon image slot it should be.
   - Options include: `MAIN`, `PT01...`, `LC01...`, and so on.
5. Click "Pack!".
6. The tool creates ZIP files named for Amazon's mass upload process.

## 🖼️ Image Naming Convention

Each ZIP contains images named according to Amazon’s convention (e.g., `ASIN.SLOT.jpg`), ensuring compatibility with bulk upload tools.

## 🔒 Privacy & Security

No internet connection is required. All processing is done locally on your machine.

## Contributing
Contributions are welcome! Feel free to submit issues or pull requests.

## 📄 License

This project is licensed under the [MIT License](LICENSE.txt).

[!["Buy Me A Coffee"](https://www.buymeacoffee.com/assets/img/custom_images/orange_img.png)](https://buymeacoffee.com/spreed)