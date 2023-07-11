#include <Keyboard.h>
#include <KeyboardLayout.h>

#define RECV_BUFFER_SIZE 600

const int OPCODE_KEEPALIVE = 0x1;
const int OPCODE_ENTER = 0x2;
const int OPCODE_INTERRUPT = 0x3;

// For transfer efficiency and speed, using binary formats
// Payload ends with '\n'

typedef struct __payload {
  uint8_t opcode;
} PAYLOAD, *PPAYLOAD;

typedef struct __keepalive {
  uint8_t opcode;
  uint16_t id;
} KEEPALIVE, *PKEEPALIVE;

typedef struct __enter {
  uint8_t opcode;
  uint16_t keycount;
  uint16_t pressTime; // Delay between down-and-up
  uint16_t pressTimeRandom;
  uint16_t keyDelay; // Delay between up(KeyA)-and-down(KeyB)
  uint16_t keyDelayRandom;
  uint16_t msg[512];
} ENTER, *PENTER;

typedef struct __interrupt {
  uint8_t opcode;
} INTERRUPT, *PINTERRUPT;

uint8_t recvBuffer[RECV_BUFFER_SIZE];

void zeroRecvBuffer() { memset(recvBuffer, 0, sizeof(uint8_t) * RECV_BUFFER_SIZE); }

void respondKeepAlive(int id)
{
  KEEPALIVE ka = { OPCODE_KEEPALIVE, id };
  Serial.write((uint8_t*)
}

void setup() {
  Keyboard.begin();
}

void loop() {
  int read = Serial.readBytesUntil('\n', recvBuffer, 600);
  if (read > 0)
  {
    PPAYLOAD payload = (PPAYLOAD)recvBuffer;
    switch(payload->opcode)
    {
      case OPCODE_KEEPALIVE:
        respondKeepAlive();
        sendBuffer
    }
  }
  memset(recvBuffer, 0, 600 * sizeof(uint8_t));
}
