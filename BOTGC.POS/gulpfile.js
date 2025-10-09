const gulp = require('gulp');
const concat = require('gulp-concat');
const terser = require('gulp-terser');
const rename = require('gulp-rename');
const gulpSass = require('gulp-sass')(require('sass'));

const paths = {
    vendor: [
        './wwwroot/lib/jquery/dist/jquery.min.js',
        './wwwroot/lib/bootstrap/dist/js/bootstrap.bundle.min.js',
    ],
    app: [
        './wwwroot/js/site.js',
    ],
    wastage: [
        './wwwroot/js/wastagesheet.js'
    ],
    stocktake: [
        './wwwroot/js/stocktake.js'
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
        .pipe(terser())
        .pipe(rename({ suffix: '.min' }))
        .pipe(gulp.dest(outputPath));
}

function appScripts() {
    return gulp.src(paths.app)
        .pipe(concat('site.bundle.js'))
        .pipe(gulp.dest(outputPath))
        .pipe(terser({ ecma: 2018 }))
        .pipe(rename({ suffix: '.min' }))
        .pipe(gulp.dest(outputPath));
}
function wastageScripts() {
    return gulp.src(paths.wastage)
        .pipe(concat('wastage.bundle.js'))
        .pipe(gulp.dest(outputPath))
        .pipe(terser({ ecma: 2018 }))
        .pipe(rename({ suffix: '.min' }))
        .pipe(gulp.dest(outputPath));
}

function stocktakeScripts() {
    return gulp.src(paths.stocktake)
        .pipe(concat('stocktake.bundle.js'))
        .pipe(gulp.dest(outputPath))
        .pipe(terser({ ecma: 2018 }))
        .pipe(rename({ suffix: '.min' }))
        .pipe(gulp.dest(outputPath));
}

function wastageSassTask() {
    return gulp.src('./wwwroot/scss/wastage.scss')
        .pipe(gulpSass({ outputStyle: 'compressed' }).on('error', gulpSass.logError))
        .pipe(rename('wastage.css'))
        .pipe(gulp.dest('./wwwroot/css/'));
}

function stockTakeSassTask() {
    return gulp.src('./wwwroot/scss/stocktake.scss')
        .pipe(gulpSass({ outputStyle: 'compressed' }).on('error', gulpSass.logError))
        .pipe(rename('stocktake.css'))
        .pipe(gulp.dest('./wwwroot/css/'));
}

const build = gulp.series(
    clean,
    gulp.parallel(vendorScripts, appScripts, wastageScripts, stocktakeScripts, wastageSassTask, stockTakeSassTask) 
);

exports.default = build;
exports.wastageSass = wastageSassTask;
exports.stockTakeSass = stockTakeSassTask;
