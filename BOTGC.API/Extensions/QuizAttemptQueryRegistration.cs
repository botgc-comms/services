using BOTGC.API.Interfaces;
using BOTGC.API.Models;

namespace BOTGC.API.Extensions;

public static class QuizAttemptQueryRegistration
{
    public static IServiceCollection AddQuizAttemptQueries(this IServiceCollection services, string attemptsTableName)
    {
        services.AddAzureTableStore<QuizAttemptEntity>(attemptsTableName);
        services.AddScoped<IQuizAttemptReadStore, TableStorageQuizAttemptReadStore>();
        return services;
    }
}

public static class CourseAssessmentRegistration
{
    public static IServiceCollection AddCourseAssessmentQueries(this IServiceCollection services, string courseAssessmentsTableName)
    {
        services.AddAzureTableStore<CourseAssessmentReportEntity>(courseAssessmentsTableName);
        services.AddScoped<ICourseAssessmentReportReadStore, TableStorageCourseAssessmentReportReadStore>();
        return services;
    }
}