class Socket 
{
public:
   virtual bool connect(const struct sockaddr *address, socklen_t address_len) = 0;
   virtual Socket* accept(struct sockaddr *restrict address, socklen_t *restrict address_len) = 0;
    /* more functions */
}