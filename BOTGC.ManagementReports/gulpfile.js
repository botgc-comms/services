
const gulp = require('gulp');
const concat = require('gulp-concat');
const uglify = require('gulp-uglify');
const rename = require('gulp-rename');
const gulpSass = require('gulp-sass')(require('sass'));

const paths = {
    vendor: [
        './wwwroot/lib/jquery/dist/jquery.min.js',
        './wwwroot/lib/bootstrap/dist/js/bootstrap.bundle.min.js',
        './wwwroot/lib/jquery-validation/dist/jquery.validate.min.js',
        './wwwroot/lib/jquery-validation-unobtrusive/dist/jquery.validate.unobtrusive.min.js'
    ],
    app: [
        './wwwroot/js/prepareCategoryGroupBreakdownChart.js',
        './wwwroot/js/prepareCategoryBreakdownChart.js',
        './wwwroot/js/prepareJoinerLeaverBarChart.js',
        './wwwroot/js/prepareMembershipChart.js',
        './wwwroot/js/prepareAccRevenueChart.js',
        './wwwroot/js/prepareWaitingListChar.js',
        './wwwroot/js/sortPlayingCategories.js',
        './wwwroot/js/assignCategoryColors.js',
        './wwwroot/js/prepareRevenueChart.js',
        './wwwroot/js/prepareStackedData.js',
        './wwwroot/js/getDistinctColor.js',
    ]
};

const outputPath = './wwwroot/js/dist/';

async function clean() {
    const { deleteAsync } = await import('del');
    return deleteAsync([`${outputPath}*.js`]);
}

function vendorScripts() {
    return gulp.src(paths.vendor)
        .pipe(concat('vendor.bundle.js'))
        .pipe(gulp.dest(outputPath))
        .pipe(uglify())
        .pipe(rename({ suffix: '.min' }))
        .pipe(gulp.dest(outputPath));
}

function appScripts() {
    return gulp.src(paths.app)
        .pipe(concat('reporting.bundle.js'))
        .pipe(gulp.dest(outputPath))
        .pipe(uglify({ mangle: { toplevel: true } }))
        .pipe(rename({ suffix: '.min' }))
        .pipe(gulp.dest(outputPath));
}

function sassTask() {
    return gulp.src('./wwwroot/scss/index.scss')
        .pipe(gulpSass({ outputStyle: 'compressed' }).on('error', gulpSass.logError))
        .pipe(rename('styles.css'))
        .pipe(gulp.dest('./wwwroot/css/'));
}

const build = gulp.series(
    clean,
    gulp.parallel(vendorScripts, appScripts, sassTask)
);

exports.default = build;
exports.sass = sassTask;
