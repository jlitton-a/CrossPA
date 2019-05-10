#include <gtest/gtest.h>
#include <functional>
#include "SignalConnection.h"

using namespace Matrix::Common;

typedef Signal<int> TestSignal;

TEST(SignalConnectionTest, NoConnection_DoesNothing)
{
   //Setup
   int result = 0;
   TestSignal myEvent;

   //Test
   myEvent(10);

   //Expectations
   EXPECT_EQ(result, 0);
}

TEST(SignalConnectionTest, Connected_CallsCallback)
{
   //Setup
   int result = 0;
   TestSignal myEvent;
   auto callback = [&result](int value) { result = value; };
   auto connection = myEvent.connect(callback);

   //Test
   myEvent(10);

   //Expectations
   EXPECT_EQ(result, 10);
}
class CallbackClass
{
public:
   CallbackClass()
   {
      mValue = 0;
   }
   int mValue;
   void MyCallback(int value)
   {
      mValue = value;
   }
};

TEST(SignalConnectionTest, Connected_CallsInstanceMethod)
{
   //Setup
   TestSignal myEvent;
   CallbackClass myClass;
   auto fn = std::bind(&CallbackClass::MyCallback, &myClass, std::placeholders::_1);
   auto connection = myEvent.connect(fn);

   //Test
   myEvent(10);

   //Expectations
   EXPECT_EQ(myClass.mValue, 10);
}

TEST(SignalConnectionTest, Disconnected_DoesNothing)
{
   //Setup
   int result = 0;
   TestSignal myEvent;
   auto callback = [&result](int value) { result = value; };
   auto connection = myEvent.connect(callback);

   //Test
   connection.disconnect();
   myEvent(10);

   //Expectations
   EXPECT_EQ(result, 0);
}

