#pragma once

#include <queue>
#include <mutex>
#include <condition_variable>

namespace Matrix
{
namespace Common
{
   /// <summary>
   /// Class that can be used to add and remove items in a queue in a thread safe way.
   /// </summary>
   template <class T>
   class SafeQueue
   {
      //***************
      // Constructors/Destructors
      //***************
   public:
      /// <summary>
      /// Initializes a new instance of the <see cref="SafeQueue"/> class.
      /// If maxItems is non-zero, an attempt to add an item will be ignored if the queue has maxItems in it.
      /// </summary>
      /// <param name="maxItems">When non-zero, the maximum number of items allowed</param>
      /// <param name="queueName">Name to be used for logging</param>
      SafeQueue(size_t maxItems = 0, std::string queueName = "")
         : _queue()
         , _queueName(queueName)
         , _lock()
         , _condition()
         , _maxItems(maxItems)
         , _stopped(false)
      {
      }

      virtual ~SafeQueue(void)
      {
         Stop();
      }

      //***************
      // Fields
      //***************
   protected:
      std::queue<T> _queue;
      std::string _queueName;
      mutable std::mutex _lock;
      std::condition_variable _condition;
      size_t _maxItems;
      bool _stopped;

      //***************
      // Methods
      //***************
   public:
      /// <summary>
      /// Returns the number of items in the queue
      /// </summary>
      /// <returns>size of the queue</returns>
      size_t size() { return _queue.size(); }

      /// <summary>
      /// Adds an item to the queue using std::move
      /// </summary>
      /// <param name="item">The item to add to the queue.</param>
      /// <returns>true if it was added, false if the maximum number of items are in the queue</returns>
      bool Enqueue_Move(T item)
      {
         std::lock_guard<std::mutex> lock(_lock);
         if (_maxItems == 0 || _queue.size() < _maxItems)
         {
            _queue.push(std::move(item));
            _condition.notify_one();
            return true;
         }
         return false;
      }
      /// <summary>
      /// Adds an item to the queue
      /// </summary>
      /// <param name="item">The item to add to the queue.</param>
      /// <returns>true if it was added, false if the maximum number of items are in the queue</returns>
      bool Enqueue(T& item)
      {
         std::lock_guard<std::mutex> lock(_lock);
         if (_maxItems == 0 || _queue.size() < _maxItems)
         {
            _queue.push(item);
            _condition.notify_one();
            return true;
         }
         return false;
      }
      /// <summary>
      /// Adds an item to the queue
      /// </summary>
      /// <param name="item">The item to add to the queue.</param>
      /// <returns>true if it was added, false if the maximum number of items are in the queue</returns>
      bool Enqueue(T&& item)
      {
         std::lock_guard<std::mutex> lock(_lock);
         if (_maxItems == 0 || _queue.size() < _maxItems)
         {
            _queue.push(item);
            _condition.notify_one();
            return true;
         }
         return false;
      }
      void Restart()
      {
         std::lock_guard<std::mutex> lock(_lock);
         _stopped = false;
      }
      void Stop()
      {
         std::lock_guard<std::mutex> lock(_lock);
         _stopped = true;
         _condition.notify_all();
      }
      /// <summary>
      /// Removes the item at the front of the queue.
      /// Blocks while the queue is empty.
      /// </summary>
      /// <param name="item">The item that was removed.</param>
      /// <returns>true if the item was removed</returns>
      bool Dequeue(T& item)
      {
         std::unique_lock<std::mutex> lock(_lock);
         while (!_stopped && _queue.empty())
         {
            _condition.wait(lock);
         }
         if (_queue.size() > 0)
         {
            item = _queue.front();
            _queue.pop();
            return true;
         }
         return false;
      }
      /// <summary>
      /// Removes the item at the front of the queue.
      /// Blocks until there is an item in the queue or until waitTimeMS time has expired.
      /// </summary>
      /// <param name="item">The item that was removed.</param>
      /// <returns>true if an item was removed, false if the timeout period expired</returns>
      bool Dequeue(T& item, int waitTimeMS)
      {
         std::unique_lock<std::mutex> lock(_lock);
         while (!_stopped && _queue.empty())
         {
            if(_condition.wait_for(lock, std::chrono::milliseconds(waitTimeMS)) == std::cv_status::timeout)
               return false;
         }
         if (_queue.size() > 0)
         {
            item = _queue.front();
            _queue.pop();
            return true;
         }
         return false;
      }
      /// <summary>
      /// Removes the item at the front of the queue.
      /// </summary>
      /// <param name="item">The item that was removed.</param>
      /// <returns>true if an item was removed, false if the queue was empty</returns>
      bool TryDequeue(T& item)
      {
         std::unique_lock<std::mutex> lock(_lock);

         if (_queue.empty())
            return false;

         item = std::move(_queue.front());
         _queue.pop();
         return true;
      }
      /// <summary>
      /// Gets the identity of this queue
      /// </summary>
      /// <returns>the identity</returns>
      std::string GetName() { return _queueName; }
      /// <summary>
      /// Gets the maximum number of items allowed in this queue
      /// </summary>
      /// <returns>the identity</returns>
      size_t GetMaxItems() { return _maxItems; }

      void Clear()
      {
         std::lock_guard<std::mutex> lock(_lock);
         _queue = std::queue<T>();
      }
   };
}
}