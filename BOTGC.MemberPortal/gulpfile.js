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
    memberPortal: [
        './wwwroot/js/member-portal.js'
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
function memberPortalScripts() {
    return gulp.src(paths.memberPortal)
        .pipe(concat('member-portal.bundle.js'))
        .pipe(gulp.dest(outputPath))
        .pipe(terser({ ecma: 2018 }))
        .pipe(rename({ suffix: '.min' }))
        .pipe(gulp.dest(outputPath));
}

function memberPortalSASSTasks() {
    return gulp.src('./wwwroot/scss/index.scss')
        .pipe(gulpSass({ outputStyle: 'compressed' }).on('error', gulpSass.logError))
        .pipe(rename('member-portal.css'))
        .pipe(gulp.dest('./wwwroot/css/'));
}

const build = gulp.series(
    clean,
    gulp.parallel(vendorScripts, appScripts, memberPortalScripts, memberPortalSASSTasks) 
);

exports.default = build;
exports.memberPortal = memberPortalSASSTasks;
