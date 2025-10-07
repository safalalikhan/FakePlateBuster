# 🚘 FakePlateBuster  
### _"No escape for number plate fakers."_

FakePlateBuster is an AI-powered prototype that detects **fraudulent or mismatched vehicle registrations** by comparing the **official (or mock) registry data** against **AI-analyzed image details**.  

This project was inspired by a real incident in Hamilton, New Zealand — where a car used a **fake number plate** that passed standard checks, but didn’t match the car’s model and color.  
FakePlateBuster uses a simple, smart approach to flag such suspicious cases automatically.

---

## 🧩 Problem Statement

Traditional number plate verification systems only check **if a plate is flagged** (e.g., stolen, expired, blacklisted).  
However, some offenders use **fake plates** that appear legitimate because they’re cloned from real vehicles.  

**FakePlateBuster** adds an extra layer of intelligence:  
👉 If the plate is valid but the **vehicle appearance (model, type, or color)** doesn’t match its record, the system flags it as **Suspicious**.

---

## 🎯 Objectives

- Detect fraudulent number plates using AI-powered image recognition.  
- Compare the car’s **visual attributes** with the **registered attributes**.  
- Flag mismatches as *Suspicious* for manual or automated review.  
- Build the prototype using a stack that matches professional standards (.NET + React + AI).

---

## ⚙️ System Architecture

### **Workflow Overview**

1. User enters a **license plate number** and uploads a **vehicle image**.  
2. Backend checks a **mock registry** (simulating NZTA data).  
3. If not flagged:
   - The image is analyzed by an **AI Vision API** (e.g., AWS Rekognition or Azure Vision).  
4. The backend compares:
   - **Registry Data:** expected make, model, color  
   - **AI Data:** detected make, type, color  
5. Returns a verdict:  
   ✅ **Match** | ⚠️ **Suspicious** | 🚫 **Flagged**

---

## 🧠 Tech Stack

| Layer | Technology |
|--------|-------------|
| **Frontend** | React.js + Bootstrap 5 |
| **Backend** | ASP.NET Core Web API (C#) |
| **AI Vision** | AWS Rekognition / Azure Computer Vision (external API) |
| **Mock Data Source** | Local JSON simulating NZTA vehicle registry |
| **Database (Future)** | PostgreSQL / SQL Server |
| **Version Control** | GitHub |
| **Logging (Future)** | Serilog / OpenTelemetry |
| **Deployment (Future)** | Azure / AWS / Render |

---

## 💾 Mock Vehicle Data Example

```json
{
  "ABC123": { "make": "Toyota", "model": "Corolla", "color": "Blue", "flagged": false },
  "XYZ987": { "make": "Honda", "model": "Civic", "color": "White", "flagged": true },
  "LMN456": { "make": "Mazda", "model": "CX-5", "color": "Red", "flagged": false }
}
