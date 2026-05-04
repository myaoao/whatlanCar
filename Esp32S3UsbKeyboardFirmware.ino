#include "USB.h"
#include "USBHIDKeyboard.h"

USBHIDKeyboard Keyboard;

// 适用：ESP32-S2 / ESP32-S3 原生 USB HID。
// Arduino IDE 里请选择支持 USB HID 的 S2/S3 开发板，并启用 USB CDC。
//
// 串口协议和 C# 端一致：
//   a-z  = 短按对应字母
//   A-Z  = 按下并保持对应字母
//   ^w   = 按下 w
//   _w   = 松开 w
//   #    = 短按空格
//   ^#   = 按下空格
//   _#   = 松开空格
//   %    = 长按空格 1 秒，单独测试用
//   !    = 释放全部按键

void setup() {
  Serial.begin(115200);
  Keyboard.begin();
  USB.begin();

  delay(1500);
  Keyboard.releaseAll();

  Serial.println("ESP32-S2/S3 USB HID Keyboard Ready");
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
    tapKey(c, 35);
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
  Keyboard.press(' ');
  delay(35);
  Keyboard.release(' ');
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
