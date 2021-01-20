#include <Wire.h> 
#define mpu_add 0x68  //mpu6050 address 
 
class kalman { 
  public : 
    double getkalman(double acc, double gyro, double dt) { 
      //project the state ahead 
      angle += dt * (gyro - bias) ; 
 
      //Project the error covariance ahead 
      P[0][0] += dt * (dt * P[1][1] - P[0][1] - P[1][0] + Q_angle) ; 
      P[0][1] -= dt * P[1][1] ; 
      P[1][0] -= dt * P[1][1] ; 
      P[1][1] += Q_gyro * dt ; 
 
      //Compute the Kalman gain 
      double S = P[0][0] + R_measure ; 
      K[0] = P[0][0] / S ; 
      K[1] = P[1][0] / S ; 
 
      //Update estimate with measurement z 
      double y = acc - angle ; 
      angle += K[0] * y ; 
      bias += K[1] * y ; 
 
      //Update the error covariance 
      double P_temp[2] = {P[0][0], P[0][1]} ; 
      P[0][0] -= K[0] * P_temp[0] ; 
      P[0][1] -= K[0] * P_temp[1] ; 
      P[1][0] -= K[1] * P_temp[0] ; 
      P[1][1] -= K[1] * P_temp[1] ; 
 
      return angle ; 
    } ; 
    void init(double angle, double gyro, double measure) { 
      Q_angle = angle ; 
      Q_gyro = gyro ; 
      R_measure = measure ; 
 
      angle = 0 ; 
      bias = 0 ; 
 
      P[0][0] = 0 ; 
      P[0][1] = 0 ; 
      P[1][0] = 0 ; 
      P[1][1] = 0 ; 
    } ; 
    double getvar(int num) { 
      switch (num) { 
        case 0 : 
          return Q_angle ; 
          break ; 
        case 1 : 
          return Q_gyro ; 
          break ; 
        case 2 : 
          return R_measure ; 
          break ; 
      } 
    } ; 
  private : 
    double Q_angle, Q_gyro, R_measure ; 
    double angle, bias ; 
    double P[2][2], K[2] ; 
} ; 
 
kalman kalx ; 
kalman kaly ; 
kalman kalz ; 
 
long ac_x, ac_y, ac_z, gy_x, gy_y, gy_z ; 
 
double deg[3], dgy_x, dgy_y, dgy_z ; 
double dt ; 
uint32_t pasttime ; 
 
int ledPin1 = 8;  // 빨강색 스위치가 눌리면 8번 핀에 연결된 LED 에 불이 들어옴 
int ledPin2 = 9;  // 파랑색 스위치가 눌리면 9번 핀에 연결된 LED 에 불이 들어옴 
int ledPin3 = 10;  // 노랑색 스위치가 눌리면 10 번 핀에 연결된 LED 에 불이 들어옴 
 
void setup() { 
  // put your setup code here, to run once: 
  Serial.begin(9600) ; 
  Wire.begin() ; 
  Wire.beginTransmission(mpu_add) ; 
  Wire.write(0x6B) ; 
  Wire.write(0) ; 
  Wire.endTransmission(true) ; 
  kalx.init(0.001, 0.003, 0.03) ;  //init kalman filter 
  kaly.init(0.001, 0.003, 0.03) ; 
  kalz.init(0.001, 0.003, 0.03) ; 
 
  pinMode(2,INPUT);  // pressed: LOW 
  pinMode(3,INPUT);  // pressed: LOW 
  pinMode(4,INPUT);  // pressed: LOW 
  pinMode(5,INPUT);  // pressed: LOW 
  pinMode(6,INPUT);  // pressed: LOW 
  pinMode(7,INPUT);  // pressed: LOW 
 
  pinMode(ledPin1, OUTPUT); 
  pinMode(ledPin2, OUTPUT); 
  pinMode(ledPin3, OUTPUT); 
} 
 
void loop() { 
  // put your main code here, to run repeatedly: 
 
  Wire.beginTransmission(mpu_add) ; //get acc data 
  Wire.write(0x3B) ; 
  Wire.endTransmission(false) ; 
  Wire.requestFrom(mpu_add, 6, true) ; 
  ac_x = Wire.read() << 8 | Wire.read() ; 
  ac_y = Wire.read() << 8 | Wire.read() ; 
  ac_z = Wire.read() << 8 | Wire.read() ; 
 
  Wire.beginTransmission(mpu_add) ; //get gyro data 
  Wire.write(0x43) ; 
  Wire.endTransmission(false) ; 
  Wire.requestFrom(mpu_add, 6, true) ; 
  gy_x = Wire.read() << 8 | Wire.read() ; 
  gy_y = Wire.read() << 8 | Wire.read() ; 
  gy_z = Wire.read() << 8 | Wire.read() ; 
 
  deg[0] = atan2(ac_x, ac_z) * 180 / PI ;  //acc data to degree data 
  deg[1] = atan2(ac_x, ac_y) * 180 / PI ; 
  deg[2] = atan2(ac_y, ac_z) * 180 / PI ; 
  dgy_x = gy_x / 131. ;  //gyro output to 
  dgy_y = gy_y / 131. ; 
  dgy_z = gy_z / 131. ; 
 
  dt = (double)(micros() - pasttime) / 1000000; 
  pasttime = micros();  //convert output to understandable data 
 
  double val[3] ; 
 
  val[0] = kalx.getkalman(deg[0], dgy_y, dt) ;  //get kalman data 
  val[1] = kaly.getkalman(deg[1], dgy_z, dt) ; 
  val[2] = kalz.getkalman(deg[2], dgy_x, dt) ; 
 
  int pass_data[2] ; 
  char sign[2] = {3, 3} ; 
 
  pass_data[0] = abs(val[0]) / 1 ;  //double to integer 
  if (val[0] < 0) sign[0] -= 2 ;  //check sign 
 
  pass_data[1] = abs(val[2]) / 1 ; 
  if (!(val[2] < 0)) sign[1] -= 2 ; 
 
  if (Serial.available()) { 
    if (Serial.read() == 's') { 
      Serial.write(0xff) ; 
      Serial.write(pass_data[0]) ; 
      Serial.write(sign[0]) ; 
      Serial.write(pass_data[1]) ; 
      Serial.write(sign[1]) ;  // 3축 자이로센서 값을 보냄 
 
      String msg;  // msg: 유니티 프로그램 측에 보낼 메시지 
 
      int state1; 
      int state2; 
      int state3;  // state1,2,3: LED 의 HIGH, LOW 상태를 저장하는 변수 
 
      if(digitalRead(2) == LOW){ 
        msg = "btn1On"; 
        state1 = HIGH; 
        state2 = LOW; 
        state3 = LOW; 
      } 
      else if(digitalRead(3) == LOW){ 
        msg = "btn2On"; 
        state1 = LOW; 
        state2 = HIGH; 
        state3 = LOW; 
      } 
      else if(digitalRead(4) == LOW){ 
        msg = "btn3On"; 
        state1 = LOW; 
        state2 = LOW; 
        state3 = HIGH; 
      } 
      else{ 
        msg = "AllOff"; 
        state1 = LOW; 
        state2 = LOW; 
        state3 = LOW; 
      } 
 
      Serial.println(msg);  // 3개의 스위치 중 어느 것이 눌렸는지를 보냄 
      Serial.flush(); 
 
      digitalWrite(ledPin1, state1); 
      digitalWrite(ledPin2, state2); 
      digitalWrite(ledPin3, state3);  // 눌린 스위치에 따라 3개의 LED 중 한 곳에 불이 들어옴 
    } 
  } 
} 