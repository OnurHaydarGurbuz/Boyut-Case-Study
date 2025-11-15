# Invoice Status Check API

Bu proje, e-fatura süreçlerine yönelik bir durum kontrol servisi uygulamasıdır. Verilen fatura numarası ve vergi numarasına göre mock bir entegratör servisi üzerinden fatura durumunu sorgular.

## Proje Hakkında

Teknik değerlendirme kapsamında geliştirilmiş bir .NET Web API'dir. Gerçek bir entegratör servisine bağlanmak yerine, test amaçlı mock veri kullanır ve istenen tüm özellikleri içerir.

## Temel Özellikler

### 1. Mock Entegratör Servisi

Gerçek bir servise bağlanmaya gerek kalmadan çalışan mock bir servis kullandım. Servis, statik bir liste üzerinden invoice number ve tax number'a göre eşleşen ilk kaydı döndürüyor. Eğer kayıt bulunamazsa varsayılan olarak REJECTED cevabı üretiliyor.

### 2. In-Memory Cache (1 Dakika)

.NET'in MemoryCache özelliğini kullandım:

- **Cache Key:** `{taxNumber}-{invoiceNumber}`
- **Süre:** 1 dakika
- Cache'de kayıt varsa direkt cache'den dönüyor, yoksa mock servisten çekip cache'e yazıyor
- Bu sayede 1 dakika içindeki tekrarlayan istekler için veritabanına gereksiz sorgu atılmıyor

### 3. Veritabanı Log Tablosu

SQLite veritabanında `INVOICE_STATUS_LOG` tablosu oluşturdum. Her API isteği sonrası mutlaka log kaydı ekleniyor:

```
INVOICE_STATUS_LOG
------------------
ID (PK)
INVOICE_NUMBER
TAX_NUMBER
RESPONSE_CODE
RESPONSE_MESSAGE
REQUEST_TIME
```

**Önemli Not:** Cache'den cevap dönse bile log kaydı ekleniyor. Bu sayede tüm isteklerin geçmişi takip edilebiliyor.

### 4. BLOCKED Mantığı

Projenin en kritik kısmı bu. İstenen şartlara göre şu mantığı uyguladım:

**Kural:** Aynı fatura numarası ve vergi numarası için 1 dakika içinde 2 kez REJECTED cevabı alınırsa, o fatura kalıcı olarak bloklanır.

**Nasıl Çalışıyor?**

1. İlk istek → REJECTED gelir → DB'ye kaydedilir
2. 1 dakika içinde ikinci istek → Yine REJECTED gelir → Sistem "1 dk içinde 2. REJECTED" tespit eder → Artık cevap BLOCKED olur
3. Bundan sonraki tüm istekler → Sürekli BLOCKED döner (sınırsız süre)

**Örnekler:**

**BLOCKED Olmaması Gereken Durum:**
```
1. istek → REJECTED
[5 dakika bekle]
2. istek → REJECTED
[5 dakika bekle]
3. istek → REJECTED
```
Aralar uzun olduğu için block tetiklenmez.

**BLOCKED Olması Gereken Durum:**
```
1. istek → REJECTED
[30 saniye bekle]
2. istek → BLOCKED (1 dk içinde 2. REJECTED!)
[10 dakika bekle]
3. istek → BLOCKED (artık sürekli)
[1 saat bekle]
4. istek → BLOCKED
```

Umarım bu mantığı doğru anlamışımdır.

### 5. CorrelationId ve Console Logları

Her istek için benzersiz bir CorrelationId üretiliyor ve console'a detaylı loglar yazılıyor:

- İsteğin kendisi
- Cache durumu (HIT/MISS)
- Mock servisten gelen cevap
- DB'ye yazılan kayıt
- BLOCKED tetiklenme durumu

Bu sayede her isteğin yaşam döngüsü baştan sona takip edilebiliyor.

## Teknolojiler

- .NET 10
- Entity Framework Core (SQLite)
- ASP.NET Core Web API
- MemoryCache
- Swagger/OpenAPI (API dokümantasyonu için)

## Veritabanını oluşturun:

```bash
dotnet ef database update
```

(Eğer migration yoksa önce `dotnet ef migrations add InitialCreate` çalıştırın)

## Projeyi çalıştırın:

```bash
dotnet run
```

## API'yi test edin:

Tarayıcıdan `https://localhost:5298/swagger` adresine gidin veya Postman kullanın.

## User API Kullanımı sonuçları:

**Request Body:**
```json
{
  "invoiceNumber": "FAT2025001",
  "taxNumber": "1234567890"
}
```

**Response (Normal - REJECTED):**
```json
{
  "status": "REJECTED",
  "message": "Hatalı imza"
}
```

**Response (Normal - APPROVED):**
```json
{
  "status": "APPROVED",
  "message": "Fatura onaylandı"
}
```

**Response (Bloklanmış Durum):**
```json
{
  "status": "BLOCKED",
  "message": "Bu faturaya ait art arda 2 red cevabı alındı. Manuel inceleme gerekiyor."
}
```
