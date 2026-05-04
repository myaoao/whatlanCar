#include <Keyboard.h>

// 串口协议：
//   a-z  = 短按对应字母
//   A-Z  = 按下并保持对应字母
//   ^w   = 按下 w
//   _w   = 松开 w
//   #    = 短按空格，使用 Keyboard.write(' ')
//   ^#   = 按下空格，使用 Keyboard.press(' ')
//   _#   = 松开空格，使用 Keyboard.release(' ')
//   %    = 长按空格 1 秒，用来单独测试空格长按
//   !    = 释放全部按键

void setup() {
  Serial.begin(115200);
  Keyboard.begin();
  Keyboard.releaseAll();

  Serial.println("Ready");
  Serial.println("a-z=tap, A-Z=hold, ^x=press, _x=release");
  Serial.println("#=tap SPACE, ^#=press SPACE, _#=release SPACE, %=hold SPACE 1s");
  Serial.println("!=release all");
}

void loop() {
  if (Serial.available() <= 0) {
    return;
  }

  char c = Serial.read();
  if (c == '\n' || c == '\r') {
    return;
  }

  if (c >= 'a' && c <= 'z') {
    tapKey(c, 30);
    Serial.print("TAP: ");
    Serial.println(c);
    return;
  }

  if (c >= 'A' && c <= 'Z') {
    char key = tolower(c);
    Keyboard.press(key);
    Serial.print("HOLD DOWN: ");
    Serial.println(key);
    return;
  }

  if (c == '#') {
    tapSpace();
    Serial.println("TAP: SPACE");
    return;
  }

  if (c == '%') {
    pressSpace();
    delay(1000);
    releaseSpace();
    Serial.println("HOLD: SPACE 1000ms");
    return;
  }

  if (c == '^') {
    char key = readNextKey();
    if (isSupportedKey(key)) {
      pressKey(key);
      Serial.print("PRESS: ");
      printKeyName(key);
    }
    return;
  }

  if (c == '_') {
    char key = readNextKey();
    if (isSupportedKey(key)) {
      releaseKey(key);
      Serial.print("RELEASE: ");
      printKeyName(key);
    }
    return;
  }

  if (c == '!') {
    Keyboard.releaseAll();
    Serial.println("RELEASE ALL");
    return;
  }

  Serial.print("UNKNOWN: ");
  Serial.println(c);
}

char readNextKey() {
  unsigned long start = millis();
  while (Serial.available() <= 0 && millis() - start < 100) {
    delay(1);
  }

  if (Serial.available() <= 0) {
    return 0;
  }

  return Serial.read();
}

bool isSupportedKey(char key) {
  return (key >= 'a' && key <= 'z') || key == '#';
}

void tapKey(char key, int delayMs) {
  Keyboard.press(key);
  delay(delayMs);
  Keyboard.release(key);
}

void pressKey(char key) {
  if (key == '#') {
    pressSpace();
  } else {
    Keyboard.press(key);
  }
}

void releaseKey(char key) {
  if (key == '#') {
    releaseSpace();
  } else {
    Keyboard.release(key);
  }
}

void tapSpace() {
  // Keyboard.write(' ') 在多数 Arduino HID 核心里比 KEY_SPACE 更稳。
  Keyboard.write(' ');
}

void pressSpace() {
  Keyboard.press(' ');
}

void releaseSpace() {
  Keyboard.release(' ');
}

void printKeyName(char key) {
  if (key == '#') {
    Serial.println("SPACE");
  } else {
    Serial.println(key);
  }
}
