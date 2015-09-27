#pragma once

#include <opencv2/opencv.hpp>

using namespace System;
using namespace System::Drawing;
using namespace System::Collections::Generic;

namespace OpenCv {

	public ref class CascadeClassifier
	{
	public:
		property bool Loaded{
			bool get(){ return loaded; }
		}

	private:
		cv::CascadeClassifier* cascade;
		bool loaded;

	public:
		CascadeClassifier(System::String^ cascadeFileName){
			std::string filename = convertToStdString(cascadeFileName);
			cascade = new cv::CascadeClassifier();

			loaded = cascade->load(filename);
		}

	public:
		List<Rectangle>^ DetectMultiScale(Bitmap^ bitmap, double scaleFactor, int minNeighbors, Size minSize, Size maxSize){
			auto iplImage = getIplImage(bitmap);
			cv::Mat image = cv::cvarrToMat(iplImage);

			cv::Mat gray;
			cv::cvtColor(image, gray, CV_BGR2GRAY);
			cvReleaseImage(&iplImage);

			std::vector<cv::Rect> objs;

			cascade->detectMultiScale(gray, objs,
				scaleFactor, minNeighbors,
				CV_HAAR_SCALE_IMAGE,
				cv::Size(minSize.Width, minSize.Height),
				cv::Size(maxSize.Width, maxSize.Height));

			std::vector<cv::Rect>::const_iterator r = objs.begin();

			List<Rectangle>^ rectangles = gcnew List<Rectangle>();
			for (; r != objs.end(); ++r) {
				Rectangle rect = Rectangle(r->x, r->y, r->width, r->height);
				rectangles->Add(rect);
			}

			return rectangles;
		}

	private:
		IplImage* getIplImage(Bitmap^ bitmap){
			Drawing::Imaging::BitmapData^ data;

			IplImage *image = cvCreateImage(cvSize(bitmap->Width, bitmap->Height), IPL_DEPTH_8U, 3);

			data = bitmap->LockBits(
				Drawing::Rectangle(0, 0, bitmap->Width, bitmap->Height),
				Drawing::Imaging::ImageLockMode::ReadOnly,
				Drawing::Imaging::PixelFormat::Format24bppRgb
				);

			memcpy(image->imageData, data->Scan0.ToPointer(), image->imageSize);

			bitmap->UnlockBits(data);
			return image;
		}

	private:
		std::string convertToStdString(String^ str)
		{
			std::string stdStr;
			if (str != nullptr &&  str->Length > 0){
				array<Byte>^ data = System::Text::Encoding::Convert(
					System::Text::Encoding::Unicode,
					System::Text::Encoding::Default,
					System::Text::Encoding::Unicode->GetBytes(str));
				pin_ptr<Byte> pin = &data[0];
				stdStr.assign(reinterpret_cast<char*>(pin), data->Length);
			}
			return stdStr;
		}
	};
}
