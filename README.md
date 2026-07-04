# PLCSharp PLC风格的上位机

用户可以通过自由配置硬件、全局变量，界面、流程完成项目开发。
持续开发中。
技术支流QQ群：109869009
 
## 简介

```
 
 Core/                       # 基础设施层
 ├── Prism/                  #   Prism框架相关
 ├── Common/                 #   通用数据模型
 ├── Tools/                  #   工具类 
 ├── UserControls/           #   控件  
 └── Resources/              #   图标字体
 Models/                     # 全局数据模型
 VVMs/                       # 业务模块 (View-ViewModel)
 ├── Authority/              #   用户认证与登录
 ├── MainWindow/             #   主窗口壳
 ├── Homepage/               #   画布 
 ├── Connects/               #   通讯  
 ├── MotionController/       #   运动控制 
 ├── Vision/                 #   机器视觉  
 ├── Workflows/              #   工作流引擎  
 ├── ModeState/              #   模式状态机
 ├── GlobalVariables/        #   全局变量管理
 ├── Recipe/                 #   配方管理
 ├── Robots/                 #   机器人配置
 └── Projects/               #   项目管理
 

```
## 免责声明

本项目仅用于学习交流，未经严格测试及安全认证，请勿用于实际生产环境，本人不对使用或无法使用该软件所造成的所有直接和间接后果包括并不限于人身伤害、财产损坏、数据丢失、错误输出、经济损失负责。