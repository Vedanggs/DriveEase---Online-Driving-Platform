# ── Stage 1: restore ─────────────────────────────────────────────────────────
# Copy only the .csproj files first so Docker layer-caches the restore step.
# A source-code change won't invalidate the cached restore layer.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
WORKDIR /repo

COPY src/DriveEase.Api/DriveEase.Api.csproj                                                                       src/DriveEase.Api/
COPY src/DriveEase.Shared/DriveEase.Shared.csproj                                                                 src/DriveEase.Shared/
COPY src/Modules/Enrollments/DriveEase.Enrollments.Application/DriveEase.Enrollments.Application.csproj          src/Modules/Enrollments/DriveEase.Enrollments.Application/
COPY src/Modules/Enrollments/DriveEase.Enrollments.Domain/DriveEase.Enrollments.Domain.csproj                    src/Modules/Enrollments/DriveEase.Enrollments.Domain/
COPY src/Modules/Enrollments/DriveEase.Enrollments.Infrastructure/DriveEase.Enrollments.Infrastructure.csproj    src/Modules/Enrollments/DriveEase.Enrollments.Infrastructure/
COPY src/Modules/Lessons/DriveEase.Lessons.Application/DriveEase.Lessons.Application.csproj                      src/Modules/Lessons/DriveEase.Lessons.Application/
COPY src/Modules/Lessons/DriveEase.Lessons.Domain/DriveEase.Lessons.Domain.csproj                                src/Modules/Lessons/DriveEase.Lessons.Domain/
COPY src/Modules/Lessons/DriveEase.Lessons.Infrastructure/DriveEase.Lessons.Infrastructure.csproj                src/Modules/Lessons/DriveEase.Lessons.Infrastructure/
COPY src/Modules/Notifications/DriveEase.Notifications.Application/DriveEase.Notifications.Application.csproj   src/Modules/Notifications/DriveEase.Notifications.Application/
COPY src/Modules/Notifications/DriveEase.Notifications.Domain/DriveEase.Notifications.Domain.csproj              src/Modules/Notifications/DriveEase.Notifications.Domain/
COPY src/Modules/Notifications/DriveEase.Notifications.Infrastructure/DriveEase.Notifications.Infrastructure.csproj src/Modules/Notifications/DriveEase.Notifications.Infrastructure/
COPY src/Modules/Schools/DriveEase.Schools.Application/DriveEase.Schools.Application.csproj                      src/Modules/Schools/DriveEase.Schools.Application/
COPY src/Modules/Schools/DriveEase.Schools.Domain/DriveEase.Schools.Domain.csproj                                src/Modules/Schools/DriveEase.Schools.Domain/
COPY src/Modules/Schools/DriveEase.Schools.Infrastructure/DriveEase.Schools.Infrastructure.csproj                src/Modules/Schools/DriveEase.Schools.Infrastructure/
COPY src/Modules/Students/DriveEase.Students.Application/DriveEase.Students.Application.csproj                   src/Modules/Students/DriveEase.Students.Application/
COPY src/Modules/Students/DriveEase.Students.Domain/DriveEase.Students.Domain.csproj                             src/Modules/Students/DriveEase.Students.Domain/
COPY src/Modules/Students/DriveEase.Students.Infrastructure/DriveEase.Students.Infrastructure.csproj             src/Modules/Students/DriveEase.Students.Infrastructure/

RUN dotnet restore src/DriveEase.Api/DriveEase.Api.csproj

# ── Stage 2: publish ──────────────────────────────────────────────────────────
FROM restore AS publish
COPY src/ src/
RUN dotnet publish src/DriveEase.Api/DriveEase.Api.csproj \
        --no-restore \
        -c Release \
        -o /app/publish

# ── Stage 3: runtime image ────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Use the non-root 'app' user that the official ASP.NET images ship with.
USER app

COPY --from=publish --chown=app /app/publish .

# Bind to port 8080 (non-privileged, suits the non-root user).
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "DriveEase.Api.dll"]
