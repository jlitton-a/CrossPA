#pragma once

///
namespace boost {
   template<class T> const T* get_pointer(const std::shared_ptr<T>& ptr)
   {
      return ptr.get();
   }

   template<class T> T* get_pointer(std::shared_ptr<T>& ptr)
   {
      return ptr.get();
   }
};
template <typename Base>
inline std::shared_ptr<Base>
shared_from_base(std::enable_shared_from_this<Base>* base)
{
   return base->shared_from_this();
}
template <typename Base>
inline std::shared_ptr<const Base>
shared_from_base(std::enable_shared_from_this<Base> const* base)
{
   return base->shared_from_this();
}
template <typename That>
inline std::shared_ptr<That>
shared_from(That* that)
{
   return std::static_pointer_cast<That>(shared_from_base(that));
}
