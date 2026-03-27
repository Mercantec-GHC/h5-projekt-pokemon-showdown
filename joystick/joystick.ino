#include <Arduino_MKRIoTCarrier.h>
#include <WiFiNINA.h>
#include <PubSubClient.h>
#include <ArduinoJson.h>

const char ssid[] = "Redmi Note 7";
const char password[] = "Billum04";

const char mqtt_server[] = "5cd80438d8ff49d99af9926fe3f099c1.s1.eu.hivemq.cloud";
const int mqtt_port = 8883;
const char mqtt_username[] = "Silasbillum";
const char mqtt_password[] = "Silasbillum1";
const char mqtt_topic[] = "game/input";

WiFiSSLClient wifiClient;
PubSubClient client(wifiClient);

const int VRx = A0;
const int VRy = A1;
const int SW = 2;

int center = 512;
int threshold = 120;

int centerX;
int centerY;

String lastDirection = "";

void reconnect() {
  while (!client.connected()) {
    Serial.println("Connecting to MQTT...");
    if (client.connect("ArduinoMKR", mqtt_username, mqtt_password)) {
      Serial.println("Connected!");
    } else {
      Serial.print("Failed: ");
      Serial.println(client.state());
      delay(2000);
    }
  }
}

void sendMQTTMessage(String direction) {
  StaticJsonDocument<128> doc;
  doc["direction"] = direction;

  char buffer[128];
  serializeJson(doc, buffer);

  client.publish(mqtt_topic, buffer);
}

void setup() {
  Serial.begin(9600);
delay(2000);

centerX = analogRead(VRx);
centerY = analogRead(VRy);

Serial.print("Center X: "); Serial.println(centerX);
Serial.print("Center Y: "); Serial.println(centerY);
  pinMode(SW, INPUT_PULLUP);

  WiFi.begin(ssid, password);
  Serial.print("Connecting WiFi");

  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
  }

  Serial.println("\nWiFi connected");

  client.setServer(mqtt_server, mqtt_port);
}

void loop() {
  if (!client.connected()) {
    reconnect();
  }

  client.loop();

  int x = analogRead(VRx);
  int y = analogRead(VRy);

  String direction = "Idle";

  int threshold = 120; 

  // Check BOTH axes independently (important!)
  bool moved = false;

  if (x > centerX + threshold) {
    direction = "Down";
    moved = true;
  }
  else if (x < centerX - threshold) {
    direction = "Up";
    moved = true;
  }

  if (y > centerY + threshold) {
    direction = "Left";
    moved = true;
  }
  else if (y < centerY - threshold) {
    direction = "Right";
    moved = true;
  }

  
  if (!moved) {
    direction = "Idle";
  }

 
  if (direction != lastDirection) {
    Serial.println("Direction: " + direction);
    sendMQTTMessage(direction);
    lastDirection = direction;
  }

  delay(100);
}