#!/bin/bash

echo "🚀 Generating remaining layers..."

# Create Application layer structure
mkdir -p src/NunchakuClub.Application/{Common/{Interfaces,Models,Mappings,Behaviors},Features/{Auth/{Commands,Queries,DTOs},Posts/{Commands,Queries,DTOs},Pages/{Commands,Queries,DTOs}}}

# Create Infrastructure layer structure
mkdir -p src/NunchakuClub.Infrastructure/{Data/{Contexts,Configurations,Repositories},Services/{CloudStorage,Caching,Authentication,Email},Identity}

# Create API layer structure
mkdir -p src/NunchakuClub.API/{Controllers,Filters,Middlewares,Extensions}

# Create Shared layer
mkdir -p src/NunchakuClub.Shared/{Constants,Extensions,Helpers}

echo "✅ Folder structure created"

# Application.csproj
cat > src/NunchakuClub.Application/NunchakuClub.Application.csproj << 'EOF'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\NunchakuClub.Domain\NunchakuClub.Domain.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="MediatR" Version="12.2.0" />
    <PackageReference Include="AutoMapper" Version="12.0.1" />
    <PackageReference Include="AutoMapper.Extensions.Microsoft.DependencyInjection" Version="12.0.1" />
    <PackageReference Include="FluentValidation" Version="11.9.0" />
    <PackageReference Include="FluentValidation.DependencyInjectionExtensions" Version="11.9.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
    <PackageReference Include="Microsoft.AspNetCore.Http.Abstractions" Version="2.2.0" />
  </ItemGroup>
</Project>
EOF

# Infrastructure.csproj
cat > src/NunchakuClub.Infrastructure/NunchakuClub.Infrastructure.csproj << 'EOF'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\NunchakuClub.Application\NunchakuClub.Application.csproj" />
    <ProjectReference Include="..\NunchakuClub.Domain\NunchakuClub.Domain.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.0" />
    <PackageReference Include="BCrypt.Net-Next" Version="4.0.3" />
    <PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="7.0.3" />
    <PackageReference Include="Microsoft.IdentityModel.Tokens" Version="7.0.3" />
    <PackageReference Include="AWSSDK.S3" Version="3.7.305" />
    <PackageReference Include="StackExchange.Redis" Version="2.7.10" />
    <PackageReference Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="8.0.0" />
  </ItemGroup>
</Project>
EOF

# API.csproj
cat > src/NunchakuClub.API/NunchakuClub.API.csproj << 'EOF'
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <RootNamespace>NunchakuClub.API</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\NunchakuClub.Application\NunchakuClub.Application.csproj" />
    <ProjectReference Include="..\NunchakuClub.Infrastructure\NunchakuClub.Infrastructure.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="8.0.0" />
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.0" />
    <PackageReference Include="Serilog.AspNetCore" Version="8.0.0" />
    <PackageReference Include="Serilog.Sinks.Console" Version="5.0.1" />
    <PackageReference Include="Serilog.Sinks.File" Version="5.0.0" />
    <PackageReference Include="AspNetCoreRateLimit" Version="5.0.0" />
    <PackageReference Include="Microsoft.AspNetCore.ResponseCompression" Version="2.2.0" />
  </ItemGroup>
</Project>
EOF

# Shared.csproj
cat > src/NunchakuClub.Shared/NunchakuClub.Shared.csproj << 'EOF'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
EOF

echo "✅ All .csproj files created"

