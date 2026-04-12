# StravaAPILibary

A comprehensive .NET library for interacting with the Strava API, providing easy-to-use methods for authentication, data retrieval, and activity management.

## 🚀 Quick Start

### Installation

Add the library to your project:

```bash
dotnet add package StravaAPILibary
```

### Basic Usage

```csharp
using StravaAPILibary.Authentication;
using StravaAPILibary.API;

// 1. Set up credentials
var credentials = new Credentials("your_client_id", "your_client_secret", "read,activity:read_all");

// 2. Authenticate
var userAuth = new UserAuthentication(credentials, "http://localhost:8080/callback", "read,activity:read_all");
userAuth.StartAuthorization();

// 3. Get authorization code from browser and exchange for token
string authCode = "your_auth_code";
bool success = await userAuth.ExchangeCodeForTokenAsync(authCode);

if (success)
{
    string accessToken = credentials.AccessToken;
    
    // 4. Use the API
    var activities = await Activities.GetAthletesActivitiesAsync(accessToken, page: 1, perPage: 10);
    var profile = await Athletes.GetAuthenticatedAthleteProfileAsync(accessToken);
}
```

## 📚 Documentation

- **[API Reference](https://deltatoolbox.github.io/StravaAPILibary/api/)** - Complete API documentation
- **[Getting Started Guide](https://deltatoolbox.github.io/StravaAPILibary/articles/getting-started/)** - Detailed setup instructions
- **[Authentication Guide](https://deltatoolbox.github.io/StravaAPILibary/articles/authentication/)** - OAuth flow and token management
- **[Examples](https://deltatoolbox.github.io/StravaAPILibary/articles/examples/)** - Common usage patterns

## 🔧 Features

### Authentication
- **OAuth 2.0 Flow** - Complete authentication implementation
- **Token Management** - Automatic token refresh and expiration handling
- **Scope Management** - Support for all Strava API scopes

### API Coverage
- **Activities** - Retrieve, upload, and update activities
- **Athletes** - Profile information and statistics
- **Clubs** - Club membership and activities
- **Gears** - Bike and shoe information
- **Routes** - Route data and GPX/TCX export
- **Segments** - Segment information and exploration
- **Streams** - Detailed time-series data
- **Uploads** - Activity file upload and status tracking

### Data Models
- **Complete Model Coverage** - All Strava API response models
- **Strongly Typed** - Full IntelliSense support
- **JSON Integration** - Seamless JSON handling

## 🛠️ Requirements

- .NET 8.0 or later
- Internet connection for API calls
- Strava API credentials (Client ID and Secret)

## 📦 NuGet Package

```xml
<PackageReference Include="StravaAPILibary" Version="1.0.0" />
```

## 🔐 Authentication Setup

1. **Create a Strava Application**
   - Visit [Strava API Settings](https://www.strava.com/settings/api)
   - Create a new application
   - Note your Client ID and Client Secret

2. **Configure Redirect URI**
   - Set your redirect URI (e.g., `http://localhost:8080/callback`)
   - Ensure it matches your application settings

3. **Request Appropriate Scopes**
   - `read` - Basic profile access
   - `activity:read_all` - Activity data access
   - `activity:write` - Upload activities
   - `profile:read_all` - Detailed profile access

## 📖 Examples

### Get Recent Activities

```csharp
var activities = await Activities.GetAthletesActivitiesAsync(accessToken, page: 1, perPage: 10);
foreach (var activity in activities)
{
    Console.WriteLine($"Activity: {activity["name"]} - {activity["distance"]}m");
}
```

### Upload an Activity

```csharp
var uploadResponse = await Activities.PostActivityAsync(
    accessToken, 
    "Morning Ride", 
    "gpx", 
    @"C:\ride.gpx",
    "Great morning ride!"
);
```

### Get Athlete Profile

```csharp
var profile = await Athletes.GetAuthenticatedAthleteProfileAsync(accessToken);
Console.WriteLine($"Athlete: {profile["firstname"]} {profile["lastname"]}");
```

### Explore Segments

```csharp
float[] bounds = { 52.4f, 13.3f, 52.6f, 13.5f }; // Berlin area
var segments = await Segments.GetExploreSegmentsAsync(accessToken, bounds, "running");
```

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guide](CONTRIBUTING.md) for details.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🔗 Links

- **[Strava API Documentation](https://developers.strava.com/docs/reference/)**
- **[Strava API Settings](https://www.strava.com/settings/api)**
- **[Issues](https://github.com/your-repo/issues)**
- **[Discussions](https://github.com/your-repo/discussions)**

## 📞 Support

- **Documentation**: [https://your-docs-site.com](https://your-docs-site.com)
- **Issues**: [GitHub Issues](https://github.com/your-repo/issues)
- **Discussions**: [GitHub Discussions](https://github.com/your-repo/discussions)

---

**Made with ❤️ for the Strava community** 
