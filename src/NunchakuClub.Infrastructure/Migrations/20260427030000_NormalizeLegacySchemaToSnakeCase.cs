using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NunchakuClub.Infrastructure.Migrations;

public partial class NormalizeLegacySchemaToSnakeCase : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        RenameTables(migrationBuilder, reverse: false);
        RenameColumns(migrationBuilder, reverse: false);
        RenameIndexes(migrationBuilder, reverse: false);
        RenameConstraints(migrationBuilder, reverse: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        RenameConstraints(migrationBuilder, reverse: true);
        RenameIndexes(migrationBuilder, reverse: true);
        RenameColumns(migrationBuilder, reverse: true);
        RenameTables(migrationBuilder, reverse: true);
    }

    private static void RenameTables(MigrationBuilder migrationBuilder, bool reverse)
    {
        var renames = new (string OldName, string NewName)[]
        {
            ("Categories", "categories"),
            ("Courses", "courses"),
            ("Pages", "pages"),
            ("Tags", "tags"),
            ("ContactSubmissions", "contact_submissions"),
            ("MenuItems", "menu_items"),
            ("ActivityLogs", "activity_logs"),
            ("Coaches", "coaches"),
            ("CourseEnrollments", "course_enrollments"),
            ("MediaFiles", "media_files"),
            ("Achievements", "achievements"),
            ("Comments", "comments"),
            ("PostImages", "post_images")
        };

        foreach (var (oldName, newName) in renames)
        {
            migrationBuilder.Sql(GetRenameTableSql(
                reverse ? newName : oldName,
                reverse ? oldName : newName));
        }
    }

    private static void RenameColumns(MigrationBuilder migrationBuilder, bool reverse)
    {
        var renames = new (string Table, string OldName, string NewName)[]
        {
            ("categories", "Id", "id"),
            ("categories", "Name", "name"),
            ("categories", "Slug", "slug"),
            ("categories", "Description", "description"),
            ("categories", "ParentId", "parent_id"),
            ("categories", "DisplayOrder", "display_order"),
            ("categories", "IsActive", "is_active"),
            ("categories", "CreatedAt", "created_at"),
            ("categories", "UpdatedAt", "updated_at"),

            ("courses", "Id", "id"),
            ("courses", "Name", "name"),
            ("courses", "Slug", "slug"),
            ("courses", "Description", "description"),
            ("courses", "Level", "level"),
            ("courses", "DurationMonths", "duration_months"),
            ("courses", "SessionsPerWeek", "sessions_per_week"),
            ("courses", "Price", "price"),
            ("courses", "IsFree", "is_free"),
            ("courses", "Features", "features"),
            ("courses", "DisplayOrder", "display_order"),
            ("courses", "IsFeatured", "is_featured"),
            ("courses", "IsActive", "is_active"),
            ("courses", "ThumbnailUrl", "thumbnail_url"),
            ("courses", "CoverImageUrl", "cover_image_url"),
            ("courses", "CreatedAt", "created_at"),
            ("courses", "UpdatedAt", "updated_at"),

            ("pages", "Id", "id"),
            ("pages", "Title", "title"),
            ("pages", "Slug", "slug"),
            ("pages", "Content", "content"),
            ("pages", "Excerpt", "excerpt"),
            ("pages", "ParentId", "parent_id"),
            ("pages", "FeaturedImageUrl", "featured_image_url"),
            ("pages", "BannerImageUrl", "banner_image_url"),
            ("pages", "MetaTitle", "meta_title"),
            ("pages", "MetaDescription", "meta_description"),
            ("pages", "DisplayOrder", "display_order"),
            ("pages", "IsPublished", "is_published"),
            ("pages", "ShowInMenu", "show_in_menu"),
            ("pages", "Template", "template"),
            ("pages", "CreatedAt", "created_at"),
            ("pages", "UpdatedAt", "updated_at"),
            ("pages", "CreatedBy", "created_by"),
            ("pages", "UpdatedBy", "updated_by"),

            ("settings", "Key", "key"),
            ("settings", "Value", "value"),
            ("settings", "Type", "type"),
            ("settings", "Description", "description"),

            ("tags", "Id", "id"),
            ("tags", "Name", "name"),
            ("tags", "Slug", "slug"),
            ("tags", "CreatedAt", "created_at"),
            ("tags", "UpdatedAt", "updated_at"),

            ("users", "Id", "id"),
            ("users", "Email", "email"),
            ("users", "Username", "username"),
            ("users", "PasswordHash", "password_hash"),
            ("users", "FullName", "full_name"),
            ("users", "Phone", "phone"),
            ("users", "AvatarUrl", "avatar_url"),
            ("users", "Role", "role"),
            ("users", "Status", "status"),
            ("users", "EmailVerified", "email_verified"),
            ("users", "LastLoginAt", "last_login_at"),
            ("users", "RefreshToken", "refresh_token"),
            ("users", "RefreshTokenExpiryTime", "refresh_token_expiry_time"),
            ("users", "CreatedAt", "created_at"),
            ("users", "UpdatedAt", "updated_at"),
            ("users", "CreatedBy", "created_by"),
            ("users", "UpdatedBy", "updated_by"),

            ("contact_submissions", "Id", "id"),
            ("contact_submissions", "FullName", "full_name"),
            ("contact_submissions", "Phone", "phone"),
            ("contact_submissions", "Email", "email"),
            ("contact_submissions", "CourseId", "course_id"),
            ("contact_submissions", "Message", "message"),
            ("contact_submissions", "Status", "status"),
            ("contact_submissions", "AdminNotes", "admin_notes"),
            ("contact_submissions", "HandledBy", "handled_by"),
            ("contact_submissions", "HandledAt", "handled_at"),
            ("contact_submissions", "IpAddress", "ip_address"),
            ("contact_submissions", "UserAgent", "user_agent"),
            ("contact_submissions", "CreatedAt", "created_at"),
            ("contact_submissions", "UpdatedAt", "updated_at"),

            ("menu_items", "Id", "id"),
            ("menu_items", "Label", "label"),
            ("menu_items", "Url", "url"),
            ("menu_items", "PageId", "page_id"),
            ("menu_items", "Target", "target"),
            ("menu_items", "ParentId", "parent_id"),
            ("menu_items", "DisplayOrder", "display_order"),
            ("menu_items", "IconClass", "icon_class"),
            ("menu_items", "MenuLocation", "menu_location"),
            ("menu_items", "IsActive", "is_active"),
            ("menu_items", "CreatedAt", "created_at"),
            ("menu_items", "UpdatedAt", "updated_at"),

            ("activity_logs", "Id", "id"),
            ("activity_logs", "UserId", "user_id"),
            ("activity_logs", "Action", "action"),
            ("activity_logs", "EntityType", "entity_type"),
            ("activity_logs", "EntityId", "entity_id"),
            ("activity_logs", "Details", "details"),
            ("activity_logs", "IpAddress", "ip_address"),
            ("activity_logs", "UserAgent", "user_agent"),
            ("activity_logs", "CreatedAt", "created_at"),
            ("activity_logs", "UpdatedAt", "updated_at"),

            ("coaches", "Id", "id"),
            ("coaches", "UserId", "user_id"),
            ("coaches", "FullName", "full_name"),
            ("coaches", "Title", "title"),
            ("coaches", "Bio", "bio"),
            ("coaches", "Specialization", "specialization"),
            ("coaches", "YearsOfExperience", "years_of_experience"),
            ("coaches", "Certifications", "certifications"),
            ("coaches", "Achievements", "achievements"),
            ("coaches", "Phone", "phone"),
            ("coaches", "Email", "email"),
            ("coaches", "AvatarUrl", "avatar_url"),
            ("coaches", "CoverImageUrl", "cover_image_url"),
            ("coaches", "DisplayOrder", "display_order"),
            ("coaches", "IsActive", "is_active"),
            ("coaches", "CreatedAt", "created_at"),
            ("coaches", "UpdatedAt", "updated_at"),

            ("course_enrollments", "Id", "id"),
            ("course_enrollments", "CourseId", "course_id"),
            ("course_enrollments", "UserId", "user_id"),
            ("course_enrollments", "FullName", "full_name"),
            ("course_enrollments", "Phone", "phone"),
            ("course_enrollments", "Email", "email"),
            ("course_enrollments", "Status", "status"),
            ("course_enrollments", "Message", "message"),
            ("course_enrollments", "AdminNotes", "admin_notes"),
            ("course_enrollments", "EnrolledAt", "enrolled_at"),
            ("course_enrollments", "ProcessedAt", "processed_at"),
            ("course_enrollments", "ProcessedBy", "processed_by"),
            ("course_enrollments", "CreatedAt", "created_at"),
            ("course_enrollments", "UpdatedAt", "updated_at"),

            ("media_files", "Id", "id"),
            ("media_files", "Filename", "filename"),
            ("media_files", "OriginalFilename", "original_filename"),
            ("media_files", "FilePath", "file_path"),
            ("media_files", "FileUrl", "file_url"),
            ("media_files", "ThumbnailUrl", "thumbnail_url"),
            ("media_files", "FileType", "file_type"),
            ("media_files", "MimeType", "mime_type"),
            ("media_files", "FileSize", "file_size"),
            ("media_files", "Width", "width"),
            ("media_files", "Height", "height"),
            ("media_files", "Duration", "duration"),
            ("media_files", "Title", "title"),
            ("media_files", "AltText", "alt_text"),
            ("media_files", "Caption", "caption"),
            ("media_files", "Description", "description"),
            ("media_files", "UploadedBy", "uploaded_by"),
            ("media_files", "UploaderId", "uploader_id"),
            ("media_files", "CreatedAt", "created_at"),
            ("media_files", "UpdatedAt", "updated_at"),

            ("posts", "Id", "id"),
            ("posts", "Title", "title"),
            ("posts", "Slug", "slug"),
            ("posts", "Excerpt", "excerpt"),
            ("posts", "Content", "content"),
            ("posts", "FeaturedImageUrl", "featured_image_url"),
            ("posts", "ThumbnailUrl", "thumbnail_url"),
            ("posts", "MetaTitle", "meta_title"),
            ("posts", "MetaDescription", "meta_description"),
            ("posts", "MetaKeywords", "meta_keywords"),
            ("posts", "Status", "status"),
            ("posts", "IsFeatured", "is_featured"),
            ("posts", "PublishedAt", "published_at"),
            ("posts", "AuthorId", "author_id"),
            ("posts", "CategoryId", "category_id"),
            ("posts", "ViewCount", "view_count"),
            ("posts", "LikeCount", "like_count"),
            ("posts", "CommentCount", "comment_count"),
            ("posts", "AdminNotes", "admin_notes"),
            ("posts", "CreatedAt", "created_at"),
            ("posts", "UpdatedAt", "updated_at"),
            ("posts", "CreatedBy", "created_by"),
            ("posts", "UpdatedBy", "updated_by"),

            ("achievements", "Id", "id"),
            ("achievements", "Title", "title"),
            ("achievements", "Description", "description"),
            ("achievements", "AchievementDate", "achievement_date"),
            ("achievements", "Type", "type"),
            ("achievements", "ImageUrl", "image_url"),
            ("achievements", "VideoUrl", "video_url"),
            ("achievements", "CoachId", "coach_id"),
            ("achievements", "ParticipantNames", "participant_names"),
            ("achievements", "DisplayOrder", "display_order"),
            ("achievements", "IsFeatured", "is_featured"),
            ("achievements", "CreatedAt", "created_at"),
            ("achievements", "UpdatedAt", "updated_at"),

            ("comments", "Id", "id"),
            ("comments", "PostId", "post_id"),
            ("comments", "UserId", "user_id"),
            ("comments", "AuthorName", "author_name"),
            ("comments", "AuthorEmail", "author_email"),
            ("comments", "Content", "content"),
            ("comments", "ParentId", "parent_id"),
            ("comments", "Status", "status"),
            ("comments", "CreatedAt", "created_at"),
            ("comments", "UpdatedAt", "updated_at"),

            ("post_tags", "PostId", "post_id"),
            ("post_tags", "TagId", "tag_id"),

            ("post_images", "Id", "id"),
            ("post_images", "PostId", "post_id"),
            ("post_images", "ImageUrl", "image_url"),
            ("post_images", "ThumbnailUrl", "thumbnail_url"),
            ("post_images", "Caption", "caption"),
            ("post_images", "AltText", "alt_text"),
            ("post_images", "DisplayOrder", "display_order"),
            ("post_images", "CreatedAt", "created_at"),
            ("post_images", "UpdatedAt", "updated_at")
        };

        foreach (var (table, oldName, newName) in renames)
        {
            migrationBuilder.Sql(GetRenameColumnSql(
                table,
                reverse ? newName : oldName,
                reverse ? oldName : newName));
        }
    }

    private static void RenameIndexes(MigrationBuilder migrationBuilder, bool reverse)
    {
        var renames = new (string OldName, string NewName)[]
        {
            ("IX_Achievements_CoachId", "ix_achievements_coach_id"),
            ("IX_ActivityLogs_UserId", "ix_activity_logs_user_id"),
            ("IX_Categories_ParentId", "ix_categories_parent_id"),
            ("IX_Coaches_UserId", "ix_coaches_user_id"),
            ("IX_Comments_ParentId", "ix_comments_parent_id"),
            ("IX_Comments_PostId", "ix_comments_post_id"),
            ("IX_Comments_UserId", "ix_comments_user_id"),
            ("IX_ContactSubmissions_CourseId", "ix_contact_submissions_course_id"),
            ("IX_CourseEnrollments_CourseId", "ix_course_enrollments_course_id"),
            ("IX_CourseEnrollments_UserId", "ix_course_enrollments_user_id"),
            ("IX_MediaFiles_UploaderId", "ix_media_files_uploader_id"),
            ("IX_MenuItems_PageId", "ix_menu_items_page_id"),
            ("IX_MenuItems_ParentId", "ix_menu_items_parent_id"),
            ("IX_Pages_ParentId", "ix_pages_parent_id"),
            ("IX_post_tags_TagId", "ix_post_tags_tag_id"),
            ("IX_PostImages_PostId", "ix_post_images_post_id"),
            ("IX_posts_AuthorId", "ix_posts_author_id"),
            ("IX_posts_CategoryId", "ix_posts_category_id"),
            ("IX_posts_IsFeatured", "ix_posts_is_featured"),
            ("IX_posts_PublishedAt", "ix_posts_published_at"),
            ("IX_posts_Slug", "ix_posts_slug"),
            ("IX_posts_Status", "ix_posts_status"),
            ("IX_users_Email", "ix_users_email"),
            ("IX_users_Username", "ix_users_username")
        };

        foreach (var (oldName, newName) in renames)
        {
            migrationBuilder.Sql(GetRenameIndexSql(
                reverse ? newName : oldName,
                reverse ? oldName : newName));
        }
    }

    private static void RenameConstraints(MigrationBuilder migrationBuilder, bool reverse)
    {
        var renames = new (string Table, string OldName, string NewName)[]
        {
            ("achievements", "PK_Achievements", "pk_achievements"),
            ("activity_logs", "PK_ActivityLogs", "pk_activity_logs"),
            ("categories", "PK_Categories", "pk_categories"),
            ("coaches", "PK_Coaches", "pk_coaches"),
            ("comments", "PK_Comments", "pk_comments"),
            ("contact_submissions", "PK_ContactSubmissions", "pk_contact_submissions"),
            ("courses", "PK_Courses", "pk_courses"),
            ("course_enrollments", "PK_CourseEnrollments", "pk_course_enrollments"),
            ("media_files", "PK_MediaFiles", "pk_media_files"),
            ("menu_items", "PK_MenuItems", "pk_menu_items"),
            ("pages", "PK_Pages", "pk_pages"),
            ("posts", "PK_posts", "pk_posts"),
            ("post_images", "PK_PostImages", "pk_post_images"),
            ("post_tags", "PK_post_tags", "pk_post_tags"),
            ("settings", "PK_settings", "pk_settings"),
            ("tags", "PK_Tags", "pk_tags"),
            ("users", "PK_users", "pk_users"),

            ("achievements", "FK_Achievements_Coaches_CoachId", "fk_achievements_coaches_coach_id"),
            ("activity_logs", "FK_ActivityLogs_users_UserId", "fk_activity_logs_users_user_id"),
            ("categories", "FK_Categories_Categories_ParentId", "fk_categories_categories_parent_id"),
            ("coaches", "FK_Coaches_users_UserId", "fk_coaches_users_user_id"),
            ("comments", "FK_Comments_Comments_ParentId", "fk_comments_comments_parent_id"),
            ("comments", "FK_Comments_posts_PostId", "fk_comments_posts_post_id"),
            ("comments", "FK_Comments_users_UserId", "fk_comments_users_user_id"),
            ("contact_submissions", "FK_ContactSubmissions_Courses_CourseId", "fk_contact_submissions_courses_course_id"),
            ("course_enrollments", "FK_CourseEnrollments_Courses_CourseId", "fk_course_enrollments_courses_course_id"),
            ("course_enrollments", "FK_CourseEnrollments_users_UserId", "fk_course_enrollments_users_user_id"),
            ("media_files", "FK_MediaFiles_users_UploaderId", "fk_media_files_users_uploader_id"),
            ("menu_items", "FK_MenuItems_MenuItems_ParentId", "fk_menu_items_menu_items_parent_id"),
            ("menu_items", "FK_MenuItems_Pages_PageId", "fk_menu_items_pages_page_id"),
            ("pages", "FK_Pages_Pages_ParentId", "fk_pages_pages_parent_id"),
            ("posts", "FK_posts_Categories_CategoryId", "fk_posts_categories_category_id"),
            ("posts", "FK_posts_users_AuthorId", "fk_posts_users_author_id"),
            ("post_images", "FK_PostImages_posts_PostId", "fk_post_images_posts_post_id"),
            ("post_tags", "FK_post_tags_posts_PostId", "fk_post_tags_posts_post_id"),
            ("post_tags", "FK_post_tags_Tags_TagId", "fk_post_tags_tags_tag_id")
        };

        foreach (var (table, oldName, newName) in renames)
        {
            migrationBuilder.Sql(GetRenameConstraintSql(
                table,
                reverse ? newName : oldName,
                reverse ? oldName : newName));
        }
    }

    private static string GetRenameTableSql(string oldName, string newName) =>
        $"""
        DO $$
        BEGIN
            IF to_regclass('public."{oldName}"') IS NOT NULL AND to_regclass('public.{newName}') IS NULL THEN
                ALTER TABLE "{oldName}" RENAME TO {newName};
            END IF;
        END
        $$;
        """;

    private static string GetRenameColumnSql(string table, string oldName, string newName) =>
        $"""
        DO $$
        BEGIN
            IF EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE table_schema = 'public'
                  AND table_name = '{table}'
                  AND column_name = '{oldName}')
               AND NOT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE table_schema = 'public'
                  AND table_name = '{table}'
                  AND column_name = '{newName}')
            THEN
                ALTER TABLE {table} RENAME COLUMN "{oldName}" TO {newName};
            END IF;
        END
        $$;
        """;

    private static string GetRenameIndexSql(string oldName, string newName) =>
        $"""
        DO $$
        BEGIN
            IF to_regclass('public."{oldName}"') IS NOT NULL AND to_regclass('public.{newName}') IS NULL THEN
                ALTER INDEX "{oldName}" RENAME TO {newName};
            END IF;
        END
        $$;
        """;

    private static string GetRenameConstraintSql(string table, string oldName, string newName) =>
        $"""
        DO $$
        BEGIN
            IF EXISTS (
                SELECT 1
                FROM pg_constraint c
                JOIN pg_class t ON t.oid = c.conrelid
                JOIN pg_namespace n ON n.oid = t.relnamespace
                WHERE n.nspname = 'public'
                  AND t.relname = '{table}'
                  AND c.conname = '{oldName}')
            THEN
                ALTER TABLE {table} RENAME CONSTRAINT "{oldName}" TO {newName};
            END IF;
        END
        $$;
        """;
}
